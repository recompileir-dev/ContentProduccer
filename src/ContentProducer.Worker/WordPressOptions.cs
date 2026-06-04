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

    public int[] CategoryIds { get; init; } = Array.Empty<int>();

    public string[] InternalLinkUrls { get; init; } = Array.Empty<string>();

    public bool SendSeoMeta { get; init; }

    public string FocusKeyphraseMetaKey { get; init; } = "_yoast_wpseo_focuskw";

    public string SeoTitleMetaKey { get; init; } = "_yoast_wpseo_title";

    public string MetaDescriptionMetaKey { get; init; } = "_yoast_wpseo_metadesc";
}
