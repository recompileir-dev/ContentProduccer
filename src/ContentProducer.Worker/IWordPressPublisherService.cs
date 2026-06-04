namespace ContentProducer.Worker;

public interface IWordPressPublisherService
{
    Task ValidateConnectionAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<WordPressMedia>> UploadImagesAsync(
        GeneratedArticle article,
        IReadOnlyList<GeneratedImage> images,
        CancellationToken cancellationToken);

    Task<WordPressPost> PublishPostAsync(
        GeneratedArticle article,
        WordPressMedia featuredImage,
        CancellationToken cancellationToken);
}
