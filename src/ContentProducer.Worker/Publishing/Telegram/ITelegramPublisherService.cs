namespace ContentProducer.Worker;

public interface ITelegramPublisherService
{
    Task PublishPostAsync(
        string caption,
        string imageUrl,
        CancellationToken cancellationToken);
}
