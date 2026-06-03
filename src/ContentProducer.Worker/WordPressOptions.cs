namespace ContentProducer.Worker;

public sealed class WordPressOptions
{
    public const string SectionName = "WordPress";

    public string SiteUrl { get; init; } = string.Empty;

    public string Username { get; init; } = string.Empty;

    public string? ApplicationPassword { get; init; }

    public string ApplicationPasswordEnvironmentVariable { get; init; } =
        "WORDPRESS_APPLICATION_PASSWORD";

    public string PostStatus { get; init; } = "publish";
}
