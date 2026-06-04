namespace ContentProducer.Worker;

public sealed class OpenAiOptions
{
    public const string SectionName = "OpenAI";

    public string? ApiKey { get; init; }

    public string ApiKeyEnvironmentVariable { get; init; } = "OPENAI_API_KEY";

    public string BaseUrl { get; init; } = "https://api.openai.com/v1/";

    public string ArticleModel { get; init; } = "gpt-5-mini";

    public string ImageModel { get; init; } = "gpt-image-1-mini";

    public bool EnableWebSearch { get; init; }

    public string ImageSize { get; init; } = "1536x1024";

    public string ImageQuality { get; init; } = "low";

    public string PromptFilePath { get; init; } = "prompts/article-simple-fa.md";

    public string ImagePromptFilePath { get; init; } = "prompts/article-image-fa.md";
}
