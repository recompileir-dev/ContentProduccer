namespace ContentProducer.Worker;

public interface IOpenAiImageService
{
    Task<IReadOnlyList<GeneratedImage>> GenerateCarouselImagesAsync(
        GeneratedArticle article,
        CancellationToken cancellationToken);
}
