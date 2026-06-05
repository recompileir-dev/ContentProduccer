namespace ContentProducer.Worker;

public sealed class ContentProductionService : IContentProductionService
{
    private readonly IContentGeneratorService _contentGeneratorService;
    private readonly IInstagramPublisherService _instagramPublisherService;
    private readonly ILogger<ContentProductionService> _logger;
    private readonly PublishingOptions _publishingOptions;
    private readonly ITelegramPublisherService _telegramPublisherService;
    private readonly WordPressOptions _wordPressOptions;
    private readonly IWordPressPublisherService _wordPressPublisherService;

    public ContentProductionService(
        IContentGeneratorService contentGeneratorService,
        IInstagramPublisherService instagramPublisherService,
        ITelegramPublisherService telegramPublisherService,
        IWordPressPublisherService wordPressPublisherService,
        Microsoft.Extensions.Options.IOptions<WordPressOptions> wordPressOptions,
        Microsoft.Extensions.Options.IOptions<PublishingOptions> publishingOptions,
        ILogger<ContentProductionService> logger)
    {
        _contentGeneratorService = contentGeneratorService;
        _instagramPublisherService = instagramPublisherService;
        _telegramPublisherService = telegramPublisherService;
        _wordPressPublisherService = wordPressPublisherService;
        _wordPressOptions = wordPressOptions.Value;
        _logger = logger;
        _publishingOptions = publishingOptions.Value;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Content production service started at {StartedAt}.",
            DateTimeOffset.Now);

        if (_publishingOptions.PublishToWordPress)
        {
            await _wordPressPublisherService.ValidateConnectionAsync(cancellationToken);
        }

        GeneratedContent content = await _contentGeneratorService.GenerateAsync(cancellationToken);
        GeneratedArticle article = content.Article;
        GeneratedImage? image = content.Image;

        if (!_publishingOptions.PublishToWordPress)
        {
            _logger.LogInformation("WordPress publishing is disabled.");
            return;
        }

        WordPressMedia? media = image is null
            ? null
            : await _wordPressPublisherService.UploadImageAsync(
                article,
                image,
                cancellationToken);

        WordPressPost wordPressPost = await _wordPressPublisherService.PublishPostAsync(
            article,
            media,
            cancellationToken);

        if (!IsPublishedWordPressPost())
        {
            _logger.LogInformation(
                "WordPress post was created with status {PostStatus}. " +
                "Social publishing is skipped because the post is not public.",
                _wordPressOptions.PostStatus);

            LogCompleted();
            return;
        }

        string instagramCaption = SocialCaptionFormatter.BuildInstagramCaption(
            article,
            wordPressPost.Link);
        string telegramCaption = SocialCaptionFormatter.BuildTelegramCaption(
            article,
            wordPressPost.Link);
        string? imageUrl = media?.SourceUrl;

        if (_publishingOptions.PublishToInstagram)
        {
            if (imageUrl is null)
            {
                _logger.LogWarning(
                    "Instagram publishing was skipped because no image was generated.");
            }
            else
            {
                await _instagramPublisherService.PublishPostAsync(
                    instagramCaption,
                    imageUrl,
                    cancellationToken);
            }
        }
        else
        {
            _logger.LogInformation("Instagram publishing is disabled.");
        }

        if (_publishingOptions.PublishToTelegram)
        {
            await _telegramPublisherService.PublishPostAsync(
                telegramCaption,
                imageUrl,
                cancellationToken);
        }
        else
        {
            _logger.LogInformation("Telegram publishing is disabled.");
        }

        LogCompleted();
    }

    private bool IsPublishedWordPressPost()
    {
        return string.Equals(
            _wordPressOptions.PostStatus,
            "publish",
            StringComparison.OrdinalIgnoreCase);
    }

    private void LogCompleted()
    {
        _logger.LogInformation(
            "Content production service completed at {CompletedAt}.",
            DateTimeOffset.Now);
    }

}
