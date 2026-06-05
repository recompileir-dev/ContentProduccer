namespace ContentProducer.Worker;

public sealed class ContentGenerationOptions
{
    public const string SectionName = "ContentGeneration";

    public string LlmProvider { get; init; } = ProviderNames.OpenAi;

    public string ImageProvider { get; init; } = ProviderNames.SourcePageImages;

    public string PromptFilePath { get; init; } = "prompts/article-news-fa.md";

    public Dictionary<string, string> ProviderPromptFilePaths { get; init; } = new();

    public string ImagePromptFilePath { get; init; } = "prompts/article-image-fa.md";
}
