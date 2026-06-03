namespace ContentProducer.Worker;

public sealed class OpenAiOptions
{
    public const string SectionName = "OpenAI";

    public string? ApiKey { get; init; }

    public string ApiKeyEnvironmentVariable { get; init; } = "OPENAI_API_KEY";

    public string BaseUrl { get; init; } = "https://api.openai.com/v1/";

    public string ArticleModel { get; init; } = "gpt-5.5";

    public string ImageModel { get; init; } = "gpt-image-2";

    public bool EnableWebSearch { get; init; } = true;

    public int ImageCount { get; init; } = 4;

    public string ImageSize { get; init; } = "1024x1024";

    public string ImageQuality { get; init; } = "medium";

    public string PromptFilePath { get; init; } = "prompts/article-news-fa.md";

    public string OutputDirectory { get; init; } = "output";
}
