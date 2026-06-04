using System.Text.Json;
using Microsoft.Extensions.Options;

namespace ContentProducer.Worker;

public sealed class ContentGeneratorService : IContentGeneratorService
{
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
        GeneratedImage image =
            await _imageService.GenerateImageAsync(article, cancellationToken);

        return new GeneratedContent(NormalizeArticle(article), image);
    }

    private async Task<GeneratedContent> LoadFixtureAsync(CancellationToken cancellationToken)
    {
        string articlePath = ResolvePath(_contentSourceOptions.FixtureArticleFilePath);
        string imagePath = ResolvePath(_contentSourceOptions.FixtureImageFilePath);

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

        if (!File.Exists(imagePath))
        {
            throw new InvalidOperationException(
                $"Fixture image file '{imagePath}' does not exist.");
        }

        byte[] imageContent = await File.ReadAllBytesAsync(imagePath, cancellationToken);
        GeneratedImage image = new(imageContent);

        _logger.LogInformation(
            "Loaded fixture article and image from {ImagePath}.",
            imagePath);

        return new GeneratedContent(NormalizeArticle(article), image);
    }

    private static GeneratedArticle NormalizeArticle(GeneratedArticle article)
    {
        string focusKeyphrase = string.IsNullOrWhiteSpace(article.FocusKeyphrase)
            ? article.Title
            : article.FocusKeyphrase;
        string seoTitle = string.IsNullOrWhiteSpace(article.SeoTitle)
            ? article.Title
            : article.SeoTitle;
        string metaDescription = string.IsNullOrWhiteSpace(article.MetaDescription)
            ? TrimToLength(article.InstagramCaption, 155)
            : article.MetaDescription;

        return article with
        {
            FocusKeyphrase = focusKeyphrase,
            SeoTitle = seoTitle,
            MetaDescription = metaDescription,
            References = article.References ?? Array.Empty<GeneratedReference>()
        };
    }

    private static string TrimToLength(string value, int maxLength)
    {
        if (value.Length <= maxLength)
        {
            return value;
        }

        return value.Substring(0, maxLength - 3) + "...";
    }

    private string ResolvePath(string path)
    {
        return Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(path, _hostEnvironment.ContentRootPath);
    }
}
