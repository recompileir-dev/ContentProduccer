namespace ContentProducer.Worker;

public sealed class GoogleImageSearchOptions
{
    public const string SectionName = "GoogleImageSearch";

    public string? ApiKey { get; init; }

    public string ApiKeyEnvironmentVariable { get; init; } =
        "GOOGLE_IMAGE_SEARCH_API_KEY";

    public string? SearchEngineId { get; init; }

    public string SearchEngineIdEnvironmentVariable { get; init; } =
        "GOOGLE_IMAGE_SEARCH_ENGINE_ID";

    public string BaseUrl { get; init; } =
        "https://www.googleapis.com/customsearch/v1";

    public string QueryTemplate { get; init; } =
        "{focusKeyphrase} {title} technology article";

    public int SearchCount { get; init; } = 10;

    public int MaxDownloadAttempts { get; init; } = 5;

    public int MaxQueryCharacters { get; init; } = 180;

    public string ImageSize { get; init; } = "large";

    public string Safe { get; init; } = "active";

    public string Rights { get; init; } = string.Empty;

    public int MaxImageBytes { get; init; } = 8_000_000;
}
