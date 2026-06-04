namespace ContentProducer.Worker;

public interface IInstagramPublisherService
{
    Task PublishPostAsync(
        string caption,
        string imageUrl,
        CancellationToken cancellationToken);
}
