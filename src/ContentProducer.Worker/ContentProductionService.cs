namespace ContentProducer.Worker;

public sealed class ContentProductionService : IContentProductionService
{
    private readonly IOpenAiArticleService _articleService;
    private readonly IOpenAiImageService _imageService;
    private readonly IInstagramPublisherService _instagramPublisherService;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILogger<ContentProductionService> _logger;
    private readonly OpenAiOptions _options;
    private readonly PublishingOptions _publishingOptions;
    private readonly ITelegramPublisherService _telegramPublisherService;
    private readonly IWordPressPublisherService _wordPressPublisherService;

    public ContentProductionService(
        IOpenAiArticleService articleService,
        IOpenAiImageService imageService,
        IInstagramPublisherService instagramPublisherService,
        ITelegramPublisherService telegramPublisherService,
        IWordPressPublisherService wordPressPublisherService,
        IHostEnvironment hostEnvironment,
        Microsoft.Extensions.Options.IOptions<OpenAiOptions> options,
        Microsoft.Extensions.Options.IOptions<PublishingOptions> publishingOptions,
        ILogger<ContentProductionService> logger)
    {
        _articleService = articleService;
        _imageService = imageService;
        _instagramPublisherService = instagramPublisherService;
        _telegramPublisherService = telegramPublisherService;
        _wordPressPublisherService = wordPressPublisherService;
        _hostEnvironment = hostEnvironment;
        _logger = logger;
        _options = options.Value;
        _publishingOptions = publishingOptions.Value;
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

        if (!_publishingOptions.PublishToWordPress)
        {
            _logger.LogInformation("WordPress publishing is disabled.");
            return;
        }

        IReadOnlyList<WordPressMedia> media = await _wordPressPublisherService.UploadImagesAsync(
            article,
            images,
            cancellationToken);

        if (media.Count == 0)
        {
            throw new InvalidOperationException("No images were uploaded to WordPress.");
        }

        WordPressPost wordPressPost = await _wordPressPublisherService.PublishPostAsync(
            article,
            media[0],
            cancellationToken);

        string instagramCaption = SocialCaptionFormatter.BuildInstagramCaption(
            article,
            wordPressPost.Link);
        string telegramCaption = SocialCaptionFormatter.BuildTelegramCaption(
            article,
            wordPressPost.Link);
        string[] imageUrls = media.Select(item => item.SourceUrl).ToArray();

        if (_publishingOptions.PublishToInstagram)
        {
            await _instagramPublisherService.PublishCarouselAsync(
                instagramCaption,
                imageUrls,
                cancellationToken);
        }
        else
        {
            _logger.LogInformation("Instagram publishing is disabled.");
        }

        if (_publishingOptions.PublishToTelegram)
        {
            await _telegramPublisherService.PublishPostAsync(
                telegramCaption,
                imageUrls,
                cancellationToken);
        }
        else
        {
            _logger.LogInformation("Telegram publishing is disabled.");
        }

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
