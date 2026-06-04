using System.Text.Json;
using Microsoft.Extensions.Options;

namespace ContentProducer.Worker;

public sealed class OpenAiImageService : IOpenAiImageService
{
    private readonly OpenAiApiClient _apiClient;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILogger<OpenAiImageService> _logger;
    private readonly OpenAiOptions _options;

    public OpenAiImageService(
        OpenAiApiClient apiClient,
        IHostEnvironment hostEnvironment,
        IOptions<OpenAiOptions> options,
        ILogger<OpenAiImageService> logger)
    {
        _apiClient = apiClient;
        _hostEnvironment = hostEnvironment;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<GeneratedImage> GenerateImageAsync(
        GeneratedArticle article,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Generating article image with OpenAI model {Model}.",
            _options.ImageModel);

        string promptTemplatePath = ResolvePath(_options.ImagePromptFilePath);
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
