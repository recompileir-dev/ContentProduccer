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
        _logger.LogInformation(
            "Generating article with Groq model {Model}.",
            _options.Model);

        using JsonDocument response = await _apiClient.PostAsync(
            "chat/completions",
            new
            {
                model = _options.Model,
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = prompt
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
            _options.Model);

        return GeneratedArticleParser.Parse(outputText, Name);
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
