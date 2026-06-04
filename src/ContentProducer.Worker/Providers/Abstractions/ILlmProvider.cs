namespace ContentProducer.Worker;

public interface ILlmProvider
{
    string Name { get; }

    Task<GeneratedArticle> GenerateArticleAsync(
        string prompt,
        CancellationToken cancellationToken);
}
