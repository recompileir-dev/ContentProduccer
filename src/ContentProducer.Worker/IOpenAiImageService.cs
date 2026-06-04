namespace ContentProducer.Worker;

public interface IOpenAiImageService
{
    Task<GeneratedImage> GenerateImageAsync(
        GeneratedArticle article,
        CancellationToken cancellationToken);
}
