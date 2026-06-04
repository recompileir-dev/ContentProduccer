namespace ContentProducer.Worker;

public interface IContentGeneratorService
{
    Task<GeneratedContent> GenerateAsync(CancellationToken cancellationToken);
}
