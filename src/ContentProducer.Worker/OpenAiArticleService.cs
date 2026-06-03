using System.Text.Json;
using Microsoft.Extensions.Options;

namespace ContentProducer.Worker;

public sealed class OpenAiArticleService : IOpenAiArticleService
{
    private readonly OpenAiApiClient _apiClient;
    private readonly ILogger<OpenAiArticleService> _logger;
    private readonly OpenAiOptions _options;

    public OpenAiArticleService(
        OpenAiApiClient apiClient,
        IOptions<OpenAiOptions> options,
        ILogger<OpenAiArticleService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<string> GenerateArticleAsync(
        string prompt,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Generating article with OpenAI model {Model}. Web search enabled: {EnableWebSearch}.",
            _options.ArticleModel,
            _options.EnableWebSearch);

        object body = _options.EnableWebSearch
            ? new
            {
                model = _options.ArticleModel,
                input = prompt,
                tools = new[] { new { type = "web_search" } }
            }
            : new
            {
                model = _options.ArticleModel,
                input = prompt
            };

        using JsonDocument response = await _apiClient.PostAsync(
            "responses",
            body,
            cancellationToken);

        string article = ExtractOutputText(response.RootElement);

        if (string.IsNullOrWhiteSpace(article))
        {
            throw new InvalidOperationException("OpenAI Responses API returned no article text.");
        }

        return article;
    }

    private static string ExtractOutputText(JsonElement root)
    {
        if (!root.TryGetProperty("output", out JsonElement output))
        {
            return string.Empty;
        }

        List<string> parts = new();

        foreach (JsonElement outputItem in output.EnumerateArray())
        {
            if (!outputItem.TryGetProperty("content", out JsonElement content))
            {
                continue;
            }

            foreach (JsonElement contentItem in content.EnumerateArray())
            {
                if (contentItem.TryGetProperty("type", out JsonElement type) &&
                    type.GetString() == "output_text" &&
                    contentItem.TryGetProperty("text", out JsonElement text))
                {
                    parts.Add(text.GetString() ?? string.Empty);
                }
            }
        }

        return string.Join(Environment.NewLine, parts);
    }
}
