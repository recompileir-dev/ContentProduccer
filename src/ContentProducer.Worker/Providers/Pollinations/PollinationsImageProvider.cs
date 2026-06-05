using Microsoft.Extensions.Options;

namespace ContentProducer.Worker;

public sealed class PollinationsImageProvider : IImageProvider
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromMinutes(10)
    };

    private readonly ContentGenerationOptions _contentGenerationOptions;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILogger<PollinationsImageProvider> _logger;
    private readonly PollinationsOptions _options;

    public PollinationsImageProvider(
        IHostEnvironment hostEnvironment,
        IOptions<ContentGenerationOptions> contentGenerationOptions,
        IOptions<PollinationsOptions> options,
        ILogger<PollinationsImageProvider> logger)
    {
        _hostEnvironment = hostEnvironment;
        _contentGenerationOptions = contentGenerationOptions.Value;
        _options = options.Value;
        _logger = logger;
    }

    public string Name => ProviderNames.Pollinations;

    public async Task<GeneratedImage?> GenerateImageAsync(
        GeneratedArticle article,
        CancellationToken cancellationToken)
    {
        string promptTemplatePath = ResolvePath(
            string.IsNullOrWhiteSpace(_options.PromptFilePath)
                ? _contentGenerationOptions.ImagePromptFilePath
                : _options.PromptFilePath);
        string promptTemplate = await File.ReadAllTextAsync(
            promptTemplatePath,
            cancellationToken);

        string articleSummary = BuildArticleSummary(article);

        string prompt = promptTemplate
            .Replace("{{title}}", article.Title, StringComparison.Ordinal)
            .Replace("{{articleHtml}}", articleSummary, StringComparison.Ordinal)
            .Replace("{{articleSummary}}", articleSummary, StringComparison.Ordinal)
            .Replace(
                "{{focusKeyphrase}}",
                WordPressContentFormatter.GetFocusKeyphrase(article),
                StringComparison.Ordinal);

        prompt = TrimToLength(prompt, _options.MaxPromptCharacters);
        Uri requestUri = BuildRequestUri(prompt);

        _logger.LogInformation(
            "Generating article image with Pollinations model {Model}. " +
            "Prompt characters: {PromptCharacters}.",
            _options.Model,
            prompt.Length);

        using HttpResponseMessage response = await SendWithRetriesAsync(
            requestUri,
            cancellationToken);
        byte[] imageContent = await response.Content.ReadAsByteArrayAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            string responseSummary = SummarizeResponse(imageContent);

            throw new InvalidOperationException(
                $"Pollinations image request failed with status " +
                $"{(int)response.StatusCode}: {responseSummary}");
        }

        string contentType = response.Content.Headers.ContentType?.MediaType
            ?? "image/jpeg";

        if (!contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            string responseSummary = SummarizeResponse(imageContent);

            throw new InvalidOperationException(
                $"Pollinations returned '{contentType}' instead of an image: " +
                responseSummary);
        }

        string fileName = GetFileName(contentType);

        _logger.LogInformation(
            "Generated Pollinations image with content type {ContentType} and " +
            "{ImageBytes} bytes.",
            contentType,
            imageContent.Length);

        return new GeneratedImage(imageContent, contentType, fileName);
    }

    private Uri BuildRequestUri(string prompt)
    {
        string baseUrl = _options.BaseUrl.EndsWith("/", StringComparison.Ordinal)
            ? _options.BaseUrl
            : _options.BaseUrl + "/";

        string url = baseUrl + Uri.EscapeDataString(prompt);
        UriBuilder builder = new(url);
        string query = string.Join(
            "&",
            $"model={Uri.EscapeDataString(_options.Model)}",
            $"width={_options.Width}",
            $"height={_options.Height}",
            $"nologo={ToQueryValue(_options.NoLogo)}",
            $"private={ToQueryValue(_options.Private)}",
            $"safe={ToQueryValue(_options.Safe)}",
            $"enhance={ToQueryValue(_options.Enhance)}");

        builder.Query = query;
        return builder.Uri;
    }

    private async Task<HttpResponseMessage> SendWithRetriesAsync(
        Uri requestUri,
        CancellationToken cancellationToken)
    {
        int maxAttempts = Math.Max(1, _options.MaxAttempts);

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            HttpResponseMessage response = await HttpClient.GetAsync(
                requestUri,
                cancellationToken);

            if (!await ShouldRetryAsync(response, cancellationToken) ||
                attempt == maxAttempts)
            {
                return response;
            }

            response.Dispose();

            TimeSpan delay = TimeSpan.FromSeconds(
                Math.Max(1, _options.RetryDelaySeconds));

            _logger.LogWarning(
                "Pollinations image request is temporarily unavailable. " +
                "Retrying attempt {NextAttempt}/{MaxAttempts} after {DelaySeconds} seconds.",
                attempt + 1,
                maxAttempts,
                delay.TotalSeconds);

            await Task.Delay(delay, cancellationToken);
        }

        throw new InvalidOperationException(
            "Pollinations image request retry loop ended unexpectedly.");
    }

    private static async Task<bool> ShouldRetryAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if ((int)response.StatusCode == 429 ||
            (int)response.StatusCode == 503)
        {
            return true;
        }

        if ((int)response.StatusCode != 402)
        {
            return false;
        }

        byte[] body = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        string text = SummarizeResponse(body);

        return text.Contains("Queue full", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildArticleSummary(GeneratedArticle article)
    {
        string summary = string.IsNullOrWhiteSpace(article.MetaDescription)
            ? article.InstagramCaption
            : article.MetaDescription;

        return WordPressContentFormatter
            .RemoveContentReferences(summary)
            .Replace("<", " <", StringComparison.Ordinal)
            .Replace(">", "> ", StringComparison.Ordinal);
    }

    private string ResolvePath(string path)
    {
        return Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(path, _hostEnvironment.ContentRootPath);
    }

    private static string TrimToLength(string value, int maxLength)
    {
        if (maxLength <= 0 || value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength];
    }

    private static string ToQueryValue(bool value)
    {
        return value ? "true" : "false";
    }

    private static string GetFileName(string contentType)
    {
        return contentType.ToLowerInvariant() switch
        {
            "image/png" => "article-image.png",
            "image/webp" => "article-image.webp",
            _ => "article-image.jpg"
        };
    }

    private static string SummarizeResponse(byte[] response)
    {
        const int maxLength = 500;

        string text = System.Text.Encoding.UTF8.GetString(response)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Trim();

        return text.Length <= maxLength
            ? text
            : text[..maxLength] + "...";
    }
}
