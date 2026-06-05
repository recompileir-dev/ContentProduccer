namespace ContentProducer.Worker;

public sealed class PollinationsOptions
{
    public const string SectionName = "Pollinations";

    public string BaseUrl { get; init; } = "https://image.pollinations.ai/prompt/";

    public string PromptFilePath { get; init; } =
        "prompts/article-image-pollinations-en.md";

    public string Model { get; init; } = "flux";

    public int Width { get; init; } = 1536;

    public int Height { get; init; } = 1024;

    public bool NoLogo { get; init; } = true;

    public bool Private { get; init; } = true;

    public bool Safe { get; init; } = true;

    public bool Enhance { get; init; }

    public int MaxPromptCharacters { get; init; } = 700;

    public int MaxAttempts { get; init; } = 3;

    public int RetryDelaySeconds { get; init; } = 30;
}
