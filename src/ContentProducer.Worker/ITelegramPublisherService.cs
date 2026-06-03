namespace ContentProducer.Worker;

public interface ITelegramPublisherService
{
    Task PublishPostAsync(
        string caption,
        IReadOnlyList<string> imageUrls,
        CancellationToken cancellationToken);
}
