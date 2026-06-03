using System.Text.Json;
using Microsoft.Extensions.Options;

namespace ContentProducer.Worker;

public sealed class OpenAiImageService : IOpenAiImageService
{
    private readonly OpenAiApiClient _apiClient;
    private readonly ILogger<OpenAiImageService> _logger;
    private readonly OpenAiOptions _options;

    public OpenAiImageService(
        OpenAiApiClient apiClient,
        IOptions<OpenAiOptions> options,
        ILogger<OpenAiImageService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<GeneratedImage>> GenerateCarouselImagesAsync(
        string article,
        CancellationToken cancellationToken)
    {
        if (_options.ImageCount < 1)
        {
            throw new InvalidOperationException("OpenAI:ImageCount must be at least 1.");
        }

        List<GeneratedImage> images = new(_options.ImageCount);

        for (int slideNumber = 1; slideNumber <= _options.ImageCount; slideNumber++)
        {
            _logger.LogInformation(
                "Generating carousel image {SlideNumber} of {ImageCount} with model {Model}.",
                slideNumber,
                _options.ImageCount,
                _options.ImageModel);

            string prompt = BuildImagePrompt(article, slideNumber, _options.ImageCount);

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
            images.Add(new GeneratedImage(slideNumber, Convert.FromBase64String(base64)));
        }

        return images;
    }

    private static string BuildImagePrompt(string article, int slideNumber, int imageCount)
    {
        return string.Join(
            Environment.NewLine,
            $"Create slide {slideNumber} of {imageCount} for a coherent Instagram carousel based on",
            "the Persian article below.",
            string.Empty,
            "Requirements:",
            "- Use a consistent modern editorial visual style across all slides.",
            "- Make the composition suitable for an Instagram square carousel.",
            "- Use strong visual storytelling and leave safe margins around important elements.",
            "- Do not include logos, watermarks, UI elements, or readable text.",
            "- Each slide must be visually distinct while clearly belonging to the same carousel.",
            string.Empty,
            "Article:",
            article);
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
