namespace ContentProducer.Worker;

public sealed class InstagramOptions
{
    public const string SectionName = "Instagram";

    public string GraphApiBaseUrl { get; init; } = "https://graph.facebook.com/";

    public string GraphApiVersion { get; init; } = "v25.0";

    public string InstagramUserId { get; init; } = string.Empty;

    public string? AccessToken { get; init; }

    public string AccessTokenEnvironmentVariable { get; init; } = "INSTAGRAM_ACCESS_TOKEN";
}
