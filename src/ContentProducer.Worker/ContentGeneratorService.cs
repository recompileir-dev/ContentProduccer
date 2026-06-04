using System.Text.Json;
using Microsoft.Extensions.Options;

namespace ContentProducer.Worker;

public sealed class ContentGeneratorService : IContentGeneratorService
{
    private static readonly string[] SupportedImageExtensions =
    {
        ".png",
        ".jpg",
        ".jpeg"
    };

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IOpenAiArticleService _articleService;
    private readonly ContentSourceOptions _contentSourceOptions;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IOpenAiImageService _imageService;
    private readonly ILogger<ContentGeneratorService> _logger;
    private readonly OpenAiOptions _openAiOptions;

    public ContentGeneratorService(
        IOpenAiArticleService articleService,
        IOpenAiImageService imageService,
        IHostEnvironment hostEnvironment,
        IOptions<ContentSourceOptions> contentSourceOptions,
        IOptions<OpenAiOptions> openAiOptions,
        ILogger<ContentGeneratorService> logger)
    {
        _articleService = articleService;
        _imageService = imageService;
        _hostEnvironment = hostEnvironment;
        _contentSourceOptions = contentSourceOptions.Value;
        _openAiOptions = openAiOptions.Value;
        _logger = logger;
    }

    public Task<GeneratedContent> GenerateAsync(CancellationToken cancellationToken)
    {
        return _contentSourceOptions.UseFixture
            ? LoadFixtureAsync(cancellationToken)
            : GenerateWithOpenAiAsync(cancellationToken);
    }

    private async Task<GeneratedContent> GenerateWithOpenAiAsync(
        CancellationToken cancellationToken)
    {
        string promptPath = ResolvePath(_openAiOptions.PromptFilePath);
        string prompt = await File.ReadAllTextAsync(promptPath, cancellationToken);
        GeneratedArticle article =
            await _articleService.GenerateArticleAsync(prompt, cancellationToken);
        IReadOnlyList<GeneratedImage> images =
            await _imageService.GenerateCarouselImagesAsync(article, cancellationToken);

        return new GeneratedContent(article, images);
    }

    private async Task<GeneratedContent> LoadFixtureAsync(CancellationToken cancellationToken)
    {
        string articlePath = ResolvePath(_contentSourceOptions.FixtureArticleFilePath);
        string imagesDirectory = ResolvePath(_contentSourceOptions.FixtureImagesDirectory);

        string articleJson = await File.ReadAllTextAsync(articlePath, cancellationToken);
        GeneratedArticle? article = JsonSerializer.Deserialize<GeneratedArticle>(
            articleJson,
            JsonOptions);

        if (article is null ||
            string.IsNullOrWhiteSpace(article.Title) ||
            string.IsNullOrWhiteSpace(article.ArticleHtml) ||
            string.IsNullOrWhiteSpace(article.InstagramCaption))
        {
            throw new InvalidOperationException(
                $"Fixture article file '{articlePath}' is invalid.");
        }

        string[] imagePaths = Directory
            .EnumerateFiles(imagesDirectory)
            .Where(path => SupportedImageExtensions.Contains(
                Path.GetExtension(path),
                StringComparer.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (imagePaths.Length == 0)
        {
            throw new InvalidOperationException(
                $"Fixture images directory '{imagesDirectory}' contains no supported images.");
        }

        List<GeneratedImage> images = new(imagePaths.Length);

        for (int index = 0; index < imagePaths.Length; index++)
        {
            byte[] content = await File.ReadAllBytesAsync(imagePaths[index], cancellationToken);
            images.Add(new GeneratedImage(index + 1, content));
        }

        _logger.LogInformation(
            "Loaded fixture article and {ImageCount} image(s) from {ImagesDirectory}.",
            images.Count,
            imagesDirectory);

        return new GeneratedContent(article, images);
    }

    private string ResolvePath(string path)
    {
        return Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(path, _hostEnvironment.ContentRootPath);
    }
}
