namespace ContentProducer.Worker;

public sealed class ContentProductionService : IContentProductionService
{
    private readonly ILogger<ContentProductionService> _logger;

    public ContentProductionService(ILogger<ContentProductionService> logger)
    {
        _logger = logger;
    }

    public Task RunAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Content production service started at {StartedAt}.",
            DateTimeOffset.Now);

        // The actual content production workflow will be added here.

        _logger.LogInformation(
            "Content production service completed at {CompletedAt}.",
            DateTimeOffset.Now);

        return Task.CompletedTask;
    }
}
