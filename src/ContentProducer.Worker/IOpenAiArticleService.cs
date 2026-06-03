namespace ContentProducer.Worker;

public interface IOpenAiArticleService
{
    Task<string> GenerateArticleAsync(string prompt, CancellationToken cancellationToken);
}
