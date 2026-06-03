namespace ContentProducer.Worker;

public interface IOpenAiArticleService
{
    Task<GeneratedArticle> GenerateArticleAsync(
        string prompt,
        CancellationToken cancellationToken);
}
