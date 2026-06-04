using System.Text.Json;
using Microsoft.Extensions.Options;

namespace ContentProducer.Worker;

public sealed class OpenAiLlmProvider : ILlmProvider
{
    private readonly OpenAiApiClient _apiClient;
    private readonly ILogger<OpenAiLlmProvider> _logger;
    private readonly OpenAiOptions _options;

    public OpenAiLlmProvider(
        OpenAiApiClient apiClient,
        IOptions<OpenAiOptions> options,
        ILogger<OpenAiLlmProvider> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
        _options = options.Value;
    }

    public string Name => ProviderNames.OpenAi;

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
                        instagramCaption = new { type = "string" },
                        focusKeyphrase = new { type = "string" },
                        seoTitle = new { type = "string" },
                        metaDescription = new { type = "string" },
                        references = new
                        {
                            type = "array",
                            items = new
                            {
                                type = "object",
                                properties = new
                                {
                                    title = new { type = "string" },
                                    url = new { type = "string" }
                                },
                                required = new[] { "title", "url" },
                                additionalProperties = false
                            }
                        }
                    },
                    required = new[]
                    {
                        "title",
                        "articleHtml",
                        "instagramCaption",
                        "focusKeyphrase",
                        "seoTitle",
                        "metaDescription",
                        "references"
                    },
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

        _logger.LogInformation(
            "Article generated with OpenAI model {Model}. Web search enabled: {EnableWebSearch}.",
            _options.ArticleModel,
            _options.EnableWebSearch);

        string outputText = ExtractOutputText(response.RootElement);

        if (string.IsNullOrWhiteSpace(outputText))
        {
            throw new InvalidOperationException("OpenAI Responses API returned no article text.");
        }

        return GeneratedArticleParser.Parse(outputText, Name);
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
