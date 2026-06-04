namespace ContentProducer.Worker;

public sealed class GroqOptions
{
    public const string SectionName = "Groq";

    public string? ApiKey { get; init; }

    public string ApiKeyEnvironmentVariable { get; init; } = "GROQ_API_KEY";

    public string BaseUrl { get; init; } = "https://api.groq.com/openai/v1/";

    public string Model { get; init; } = "groq/compound";

    public int MaxCompletionTokens { get; init; } = 4096;
}
