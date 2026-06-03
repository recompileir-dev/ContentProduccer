namespace ContentProducer.Worker;

public sealed class ContentProductionService : IContentProductionService
{
    private readonly IOpenAiArticleService _articleService;
    private readonly IOpenAiImageService _imageService;
    private readonly IInstagramPublisherService _instagramPublisherService;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILogger<ContentProductionService> _logger;
    private readonly OpenAiOptions _options;
    private readonly IWordPressPublisherService _wordPressPublisherService;

    public ContentProductionService(
        IOpenAiArticleService articleService,
        IOpenAiImageService imageService,
        IInstagramPublisherService instagramPublisherService,
        IWordPressPublisherService wordPressPublisherService,
        IHostEnvironment hostEnvironment,
        Microsoft.Extensions.Options.IOptions<OpenAiOptions> options,
        ILogger<ContentProductionService> logger)
    {
        _articleService = articleService;
        _imageService = imageService;
        _instagramPublisherService = instagramPublisherService;
        _wordPressPublisherService = wordPressPublisherService;
        _hostEnvironment = hostEnvironment;
        _logger = logger;
        _options = options.Value;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Content production service started at {StartedAt}.",
            DateTimeOffset.Now);

        string promptPath = ResolvePath(_options.PromptFilePath);
        string prompt = await File.ReadAllTextAsync(promptPath, cancellationToken);
        GeneratedArticle article =
            await _articleService.GenerateArticleAsync(prompt, cancellationToken);
        IReadOnlyList<GeneratedImage> images =
            await _imageService.GenerateCarouselImagesAsync(article, cancellationToken);

        IReadOnlyList<WordPressMedia> media =
            await _wordPressPublisherService.UploadImagesAsync(
                article,
                images,
                cancellationToken);

        if (media.Count == 0)
        {
            throw new InvalidOperationException("No images were uploaded to WordPress.");
        }

        await _wordPressPublisherService.PublishPostAsync(
            article,
            media[0],
            cancellationToken);

        await _instagramPublisherService.PublishCarouselAsync(
            article.InstagramCaption,
            media.Select(item => item.SourceUrl).ToArray(),
            cancellationToken);

        _logger.LogInformation(
            "Content production service completed at {CompletedAt}.",
            DateTimeOffset.Now);
    }

    private string ResolvePath(string path)
    {
        return Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(path, _hostEnvironment.ContentRootPath);
    }
}
