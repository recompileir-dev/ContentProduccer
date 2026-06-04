namespace ContentProducer.Worker;

public interface IWordPressPublisherService
{
    Task ValidateConnectionAsync(CancellationToken cancellationToken);

    Task<WordPressMedia> UploadImageAsync(
        GeneratedArticle article,
        GeneratedImage image,
        CancellationToken cancellationToken);

    Task<WordPressPost> PublishPostAsync(
        GeneratedArticle article,
        WordPressMedia? featuredImage,
        CancellationToken cancellationToken);
}
