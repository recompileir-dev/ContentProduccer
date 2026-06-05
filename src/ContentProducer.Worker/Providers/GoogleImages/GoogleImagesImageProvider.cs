using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace ContentProducer.Worker;

public sealed class GoogleImagesImageProvider : IImageProvider
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private static readonly HttpClient HttpClient = CreateHttpClient();

    private readonly ILogger<GoogleImagesImageProvider> _logger;
    private readonly GoogleImageSearchOptions _options;

    public GoogleImagesImageProvider(
        IOptions<GoogleImageSearchOptions> options,
        ILogger<GoogleImagesImageProvider> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public string Name => ProviderNames.GoogleImages;

    public async Task<GeneratedImage?> GenerateImageAsync(
        GeneratedArticle article,
        CancellationToken cancellationToken)
    {
        string query = BuildQuery(article);

        _logger.LogInformation(
            "Searching Google Images for article image. Query: {Query}",
            query);

        IReadOnlyList<GoogleImageResult> results =
            await SearchImagesAsync(query, cancellationToken);

        if (results.Count == 0)
        {
            throw new InvalidOperationException(
                $"Google image search returned no image results for query '{query}'.");
        }

        foreach (GoogleImageResult result in results.Take(
            Math.Max(1, _options.MaxDownloadAttempts)))
        {
            GeneratedImage? image = await TryDownloadImageAsync(
                result,
                cancellationToken);

            if (image is not null)
            {
                return image;
            }
        }

        throw new InvalidOperationException(
            "Google image search returned results, but none of the candidate " +
            "images could be downloaded as an image.");
    }

    private async Task<IReadOnlyList<GoogleImageResult>> SearchImagesAsync(
        string query,
        CancellationToken cancellationToken)
    {
        string apiKey = GetRequiredSecret(
            _options.ApiKey,
            _options.ApiKeyEnvironmentVariable,
            "Google image search API key");
        string searchEngineId = GetRequiredSecret(
            _options.SearchEngineId,
            _options.SearchEngineIdEnvironmentVariable,
            "Google image search engine ID");

        Uri requestUri = BuildSearchUri(apiKey, searchEngineId, query);

        using HttpResponseMessage response = await HttpClient.GetAsync(
            requestUri,
            cancellationToken);
        string responseBody = await response.Content.ReadAsStringAsync(
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Google image search failed with status {(int)response.StatusCode}: " +
                Summarize(responseBody));
        }

        using JsonDocument document = JsonDocument.Parse(responseBody);

        if (!document.RootElement.TryGetProperty("items", out JsonElement items))
        {
            return Array.Empty<GoogleImageResult>();
        }

        return items.EnumerateArray()
            .Select(ParseResult)
            .Where(result => Uri.TryCreate(result.ImageUrl, UriKind.Absolute, out _))
            .ToArray();
    }

    private async Task<GeneratedImage?> TryDownloadImageAsync(
        GoogleImageResult result,
        CancellationToken cancellationToken)
    {
        try
        {
            using HttpResponseMessage response = await HttpClient.GetAsync(
                result.ImageUrl,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Skipping Google image result {ImageUrl}. Download status: {StatusCode}.",
                    result.ImageUrl,
                    (int)response.StatusCode);

                return null;
            }

            string contentType = response.Content.Headers.ContentType?.MediaType
                ?? result.Mime
                ?? "application/octet-stream";

            if (!contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "Skipping Google image result {ImageUrl}. Content type: {ContentType}.",
                    result.ImageUrl,
                    contentType);

                return null;
            }

            byte[] content = await response.Content.ReadAsByteArrayAsync(
                cancellationToken);

            if (content.Length == 0 || content.Length > _options.MaxImageBytes)
            {
                _logger.LogWarning(
                    "Skipping Google image result {ImageUrl}. Image bytes: {ImageBytes}.",
                    result.ImageUrl,
                    content.Length);

                return null;
            }

            string attribution = BuildAttribution(result);

            _logger.LogInformation(
                "Selected Google image result from {SourceHost}. Content type: " +
                "{ContentType}. Bytes: {ImageBytes}.",
                result.DisplayLink,
                contentType,
                content.Length);

            return new GeneratedImage(
                content,
                contentType,
                GetFileName(contentType, result.ImageUrl),
                attribution,
                result.ImageUrl);
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(
                exception,
                "Skipping Google image result {ImageUrl}. Download failed.",
                result.ImageUrl);

            return null;
        }
    }

    private Uri BuildSearchUri(
        string apiKey,
        string searchEngineId,
        string query)
    {
        List<string> parameters = new()
        {
            $"key={Uri.EscapeDataString(apiKey)}",
            $"cx={Uri.EscapeDataString(searchEngineId)}",
            $"q={Uri.EscapeDataString(query)}",
            "searchType=image",
            $"num={Math.Clamp(_options.SearchCount, 1, 10)}",
            $"imgSize={Uri.EscapeDataString(_options.ImageSize)}",
            $"safe={Uri.EscapeDataString(_options.Safe)}"
        };

        if (!string.IsNullOrWhiteSpace(_options.Rights))
        {
            parameters.Add($"rights={Uri.EscapeDataString(_options.Rights)}");
        }

        UriBuilder builder = new(_options.BaseUrl)
        {
            Query = string.Join("&", parameters)
        };

        return builder.Uri;
    }

    private string BuildQuery(GeneratedArticle article)
    {
        string query = _options.QueryTemplate
            .Replace("{title}", article.Title, StringComparison.Ordinal)
            .Replace(
                "{focusKeyphrase}",
                WordPressContentFormatter.GetFocusKeyphrase(article),
                StringComparison.Ordinal)
            .Replace(
                "{metaDescription}",
                article.MetaDescription ?? string.Empty,
                StringComparison.Ordinal);

        query = string.Join(
            " ",
            query.Split(
                ['\r', '\n', '\t', ' '],
                StringSplitOptions.RemoveEmptyEntries));

        return query.Length <= _options.MaxQueryCharacters
            ? query
            : query[.._options.MaxQueryCharacters];
    }

    private static GoogleImageResult ParseResult(JsonElement item)
    {
        string imageUrl = GetString(item, "link");
        string title = GetString(item, "title");
        string displayLink = GetString(item, "displayLink");
        string mime = GetString(item, "mime");
        string contextLink = string.Empty;

        if (item.TryGetProperty("image", out JsonElement image))
        {
            contextLink = GetString(image, "contextLink");
        }

        return new GoogleImageResult(
            imageUrl,
            title,
            displayLink,
            mime,
            contextLink);
    }

    private static string BuildAttribution(GoogleImageResult result)
    {
        string source = string.IsNullOrWhiteSpace(result.ContextLink)
            ? result.ImageUrl
            : result.ContextLink;
        string title = string.IsNullOrWhiteSpace(result.Title)
            ? "Google image search result"
            : result.Title;

        return string.IsNullOrWhiteSpace(result.DisplayLink)
            ? $"{title} - {source}"
            : $"{title} - {result.DisplayLink} - {source}";
    }

    private static string GetRequiredSecret(
        string? configuredValue,
        string environmentVariable,
        string label)
    {
        string? value = string.IsNullOrWhiteSpace(configuredValue)
            ? Environment.GetEnvironmentVariable(environmentVariable)
            : configuredValue;

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"{label} is missing. Set '{environmentVariable}'.");
        }

        return value.Trim();
    }

    private static string GetFileName(string contentType, string imageUrl)
    {
        string extension = contentType.ToLowerInvariant() switch
        {
            "image/png" => ".png",
            "image/webp" => ".webp",
            "image/gif" => ".gif",
            _ => ".jpg"
        };

        string baseName = Path.GetFileNameWithoutExtension(
            Uri.TryCreate(imageUrl, UriKind.Absolute, out Uri? uri)
                ? uri.LocalPath
                : imageUrl);

        if (string.IsNullOrWhiteSpace(baseName))
        {
            baseName = "google-image";
        }

        return baseName + extension;
    }

    private static string GetString(JsonElement item, string propertyName)
    {
        return item.TryGetProperty(propertyName, out JsonElement property)
            ? property.GetString() ?? string.Empty
            : string.Empty;
    }

    private static string Summarize(string value)
    {
        const int maxLength = 800;

        string summary = value
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Trim();

        return summary.Length <= maxLength
            ? summary
            : summary[..maxLength] + "...";
    }

    private static HttpClient CreateHttpClient()
    {
        HttpClient client = new()
        {
            Timeout = TimeSpan.FromMinutes(2)
        };

        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.UserAgent.ParseAdd("ContentProducer/1.0");

        return client;
    }

    private sealed record GoogleImageResult(
        string ImageUrl,
        string Title,
        string DisplayLink,
        string Mime,
        string ContextLink);
}
