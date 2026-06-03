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

    public async Task<GeneratedArticle> GenerateArticleAsync(
        string prompt,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Generating article with OpenAI model {Model}. Web search enabled: {EnableWebSearch}.",
            _options.ArticleModel,
            _options.EnableWebSearch);

        object textFormat = new
        {
            format = new
            {
                type = "json_schema",
                name = "generated_article",
                strict = true,
                schema = new
                {
                    type = "object",
                    properties = new
                    {
                        title = new { type = "string" },
                        articleHtml = new { type = "string" },
                        instagramCaption = new { type = "string" }
                    },
                    required = new[] { "title", "articleHtml", "instagramCaption" },
                    additionalProperties = false
                }
            }
        };

        object body = _options.EnableWebSearch
            ? new
            {
                model = _options.ArticleModel,
                input = prompt,
                tools = new[] { new { type = "web_search" } },
                text = textFormat
            }
            : new
            {
                model = _options.ArticleModel,
                input = prompt,
                text = textFormat
            };

        using JsonDocument response = await _apiClient.PostAsync(
            "responses",
            body,
            cancellationToken);

        string outputText = ExtractOutputText(response.RootElement);

        if (string.IsNullOrWhiteSpace(outputText))
        {
            throw new InvalidOperationException("OpenAI Responses API returned no article text.");
        }

        return ParseArticle(outputText);
    }

    private static GeneratedArticle ParseArticle(string outputText)
    {
        string json = RemoveMarkdownCodeFence(outputText);

        GeneratedArticle? article = JsonSerializer.Deserialize<GeneratedArticle>(
            json,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        if (article is null ||
            string.IsNullOrWhiteSpace(article.Title) ||
            string.IsNullOrWhiteSpace(article.ArticleHtml) ||
            string.IsNullOrWhiteSpace(article.InstagramCaption))
        {
            throw new InvalidOperationException(
                "OpenAI article response must contain title, articleHtml, and instagramCaption.");
        }

        return article;
    }

    private static string RemoveMarkdownCodeFence(string text)
    {
        string trimmed = text.Trim();

        if (!trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            return trimmed;
        }

        int firstNewLine = trimmed.IndexOf('\n');
        int lastFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);

        if (firstNewLine < 0 || lastFence <= firstNewLine)
        {
            return trimmed;
        }

        return trimmed.Substring(firstNewLine + 1, lastFence - firstNewLine - 1).Trim();
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
