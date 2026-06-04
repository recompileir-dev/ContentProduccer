namespace ContentProducer.Worker;

public interface IContentProductionService
{
    Task RunAsync(CancellationToken cancellationToken);
}
