using System.Text.Json;
using Microsoft.Extensions.Options;

namespace ContentProducer.Worker;

public sealed class OpenAiImageProvider : IImageProvider
{
    private readonly OpenAiApiClient _apiClient;
    private readonly ContentGenerationOptions _contentGenerationOptions;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILogger<OpenAiImageProvider> _logger;
    private readonly OpenAiOptions _options;

    public OpenAiImageProvider(
        OpenAiApiClient apiClient,
        IHostEnvironment hostEnvironment,
        IOptions<ContentGenerationOptions> contentGenerationOptions,
        IOptions<OpenAiOptions> options,
        ILogger<OpenAiImageProvider> logger)
    {
        _apiClient = apiClient;
        _hostEnvironment = hostEnvironment;
        _contentGenerationOptions = contentGenerationOptions.Value;
        _logger = logger;
        _options = options.Value;
    }

    public string Name => ProviderNames.OpenAi;

    public async Task<GeneratedImage> GenerateImageAsync(
        GeneratedArticle article,
        CancellationToken cancellationToken)
    {
        string promptTemplatePath = ResolvePath(
            _contentGenerationOptions.ImagePromptFilePath);
        string promptTemplate = await File.ReadAllTextAsync(
            promptTemplatePath,
            cancellationToken);
        string prompt = promptTemplate
            .Replace("{{title}}", article.Title, StringComparison.Ordinal)
            .Replace("{{articleHtml}}", article.ArticleHtml, StringComparison.Ordinal)
            .Replace(
                "{{focusKeyphrase}}",
                WordPressContentFormatter.GetFocusKeyphrase(article),
                StringComparison.Ordinal);

        _logger.LogInformation(
            "Generating article image with OpenAI model {Model}. Prompt characters: " +
            "{PromptCharacters}.",
            _options.ImageModel,
            prompt.Length);

        using JsonDocument response = await _apiClient.PostAsync(
            "images/generations",
            new
            {
                model = _options.ImageModel,
                prompt,
                n = 1,
                size = _options.ImageSize,
                quality = _options.ImageQuality
            },
            cancellationToken);

        string base64 = ExtractBase64Image(response.RootElement);
        return new GeneratedImage(Convert.FromBase64String(base64));
    }

    private string ResolvePath(string path)
    {
        return Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(path, _hostEnvironment.ContentRootPath);
    }

    private static string ExtractBase64Image(JsonElement root)
    {
        if (root.TryGetProperty("data", out JsonElement data) &&
            data.GetArrayLength() > 0 &&
            data[0].TryGetProperty("b64_json", out JsonElement base64))
        {
            return base64.GetString()
                ?? throw new InvalidOperationException("OpenAI Image API returned empty image data.");
        }

        throw new InvalidOperationException("OpenAI Image API returned no image data.");
    }
}
