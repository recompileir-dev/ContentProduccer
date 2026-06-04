namespace ContentProducer.Worker;

public sealed class GroqOptions
{
    public const string SectionName = "Groq";

    public string? ApiKey { get; init; }

    public string ApiKeyEnvironmentVariable { get; init; } = "GROQ_API_KEY";

    public string BaseUrl { get; init; } = "https://api.groq.com/openai/v1/";

    public string WriterModel { get; init; } = "openai/gpt-oss-120b";

    public int MaxCompletionTokens { get; init; } = 5000;

    public int MinimumArticleHtmlCharacters { get; init; } = 4500;

    public bool RejectShortArticles { get; init; } = true;

    public bool EnableWebResearch { get; init; } = true;

    public string ResearchModel { get; init; } = "groq/compound-mini";

    public string ResearchModelVersion { get; init; } = "2025-07-23";

    public int ResearchMaxCompletionTokens { get; init; } = 2048;

    public int ResearchPromptMaxCharacters { get; init; } = 1200;

    public int MaxResearchCharacters { get; init; } = 12000;

    public bool ContinueWithoutResearchOnFailure { get; init; } = true;
}
