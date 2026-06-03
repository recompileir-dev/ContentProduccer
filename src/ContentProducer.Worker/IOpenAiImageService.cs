namespace ContentProducer.Worker;

public interface IOpenAiImageService
{
    Task<IReadOnlyList<GeneratedImage>> GenerateCarouselImagesAsync(
        string article,
        CancellationToken cancellationToken);
}
