namespace ContentProducer.Worker;

public interface IInstagramPublisherService
{
    Task PublishCarouselAsync(
        string caption,
        IReadOnlyList<string> imageUrls,
        CancellationToken cancellationToken);
}
