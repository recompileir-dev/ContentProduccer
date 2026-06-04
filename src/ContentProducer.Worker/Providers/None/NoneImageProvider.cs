namespace ContentProducer.Worker;

public sealed class NoneImageProvider : IImageProvider
{
    private readonly ILogger<NoneImageProvider> _logger;

    public NoneImageProvider(ILogger<NoneImageProvider> logger)
    {
        _logger = logger;
    }

    public string Name => ProviderNames.None;

    public Task<GeneratedImage?> GenerateImageAsync(
        GeneratedArticle article,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Image generation is disabled for article {Title}.",
            article.Title);

        return Task.FromResult<GeneratedImage?>(null);
    }
}
