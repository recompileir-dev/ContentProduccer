namespace ContentProducer.Worker;

public interface IImageProvider
{
    string Name { get; }

    Task<GeneratedImage> GenerateImageAsync(
        GeneratedArticle article,
        CancellationToken cancellationToken);
}
