using System.Text.Json;
using Microsoft.Extensions.Options;

namespace ContentProducer.Worker;

public sealed class GroqLlmProvider : ILlmProvider
{
    private readonly GroqApiClient _apiClient;
    private readonly ILogger<GroqLlmProvider> _logger;
    private readonly GroqOptions _options;

    public GroqLlmProvider(
        GroqApiClient apiClient,
        IOptions<GroqOptions> options,
        ILogger<GroqLlmProvider> logger)
    {
        _apiClient = apiClient;
        _options = options.Value;
        _logger = logger;
    }

    public string Name => ProviderNames.Groq;

    public async Task<GeneratedArticle> GenerateArticleAsync(
        string prompt,
        CancellationToken cancellationToken)
    {
        string research = _options.EnableWebResearch
            ? await ResearchAsync(prompt, cancellationToken)
            : string.Empty;
        string writerPrompt = BuildWriterPrompt(prompt, research);

        _logger.LogInformation(
            "Generating article with Groq writer model {Model}. Prompt characters: {PromptCharacters}. " +
            "Max completion tokens: {MaxCompletionTokens}.",
            _options.WriterModel,
            writerPrompt.Length,
            _options.MaxCompletionTokens);

        using JsonDocument response = await _apiClient.PostAsync(
            "chat/completions",
            new
            {
                model = _options.WriterModel,
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = writerPrompt
                    }
                },
                response_format = new
                {
                    type = "json_object"
                },
                max_completion_tokens = _options.MaxCompletionTokens
            },
            cancellationToken);

        string outputText = ExtractOutputText(response.RootElement);

        if (string.IsNullOrWhiteSpace(outputText))
        {
            throw new InvalidOperationException(
                "Groq Chat Completions API returned no article text.");
        }

        _logger.LogInformation(
            "Article generated with Groq model {Model}.",
            _options.WriterModel);

        return GeneratedArticleParser.Parse(outputText, Name);
    }

    private async Task<string> ResearchAsync(
        string prompt,
        CancellationToken cancellationToken)
    {
        string researchPrompt = string.Join(
            Environment.NewLine,
            "Research the following Persian article request using current web sources.",
            "Return concise research notes only. Include 2 or 3 relevant recent news items,",
            "their publication dates, key factual details, source titles, and direct URLs.",
            "Do not write the final article and do not return JSON.",
            string.Empty,
            prompt);

        _logger.LogInformation(
            "Researching article with Groq model {Model}. Prompt characters: {PromptCharacters}.",
            _options.ResearchModel,
            researchPrompt.Length);

        using JsonDocument response = await _apiClient.PostAsync(
            "chat/completions",
            new
            {
                model = _options.ResearchModel,
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = researchPrompt
                    }
                },
                max_completion_tokens = _options.ResearchMaxCompletionTokens
            },
            cancellationToken);

        string research = ExtractOutputText(response.RootElement);

        if (string.IsNullOrWhiteSpace(research))
        {
            throw new InvalidOperationException(
                "Groq research model returned no research notes.");
        }

        if (research.Length > _options.MaxResearchCharacters)
        {
            research = research.Substring(0, _options.MaxResearchCharacters);
        }

        _logger.LogInformation(
            "Groq research completed with {ResearchCharacters} characters.",
            research.Length);

        return research;
    }

    private static string BuildWriterPrompt(string prompt, string research)
    {
        if (string.IsNullOrWhiteSpace(research))
        {
            return prompt;
        }

        return string.Join(
            Environment.NewLine,
            prompt,
            string.Empty,
            "# یادداشت‌های تحقیق وب",
            "برای نگارش مقاله فقط از اطلاعات قابل اتکای زیر استفاده کن.",
            "لینک منابع را از همین یادداشت‌ها در آرایه references قرار بده.",
            string.Empty,
            research);
    }

    private static string ExtractOutputText(JsonElement root)
    {
        if (root.TryGetProperty("choices", out JsonElement choices) &&
            choices.GetArrayLength() > 0 &&
            choices[0].TryGetProperty("message", out JsonElement message) &&
            message.TryGetProperty("content", out JsonElement content))
        {
            return content.GetString() ?? string.Empty;
        }

        return string.Empty;
    }
}
