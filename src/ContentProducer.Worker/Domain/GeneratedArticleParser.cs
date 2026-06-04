using System.Text.Json;

namespace ContentProducer.Worker;

public static class GeneratedArticleParser
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public static GeneratedArticle Parse(string outputText, string providerName)
    {
        string json = RemoveMarkdownCodeFence(outputText);
        GeneratedArticle? article = JsonSerializer.Deserialize<GeneratedArticle>(
            json,
            JsonOptions);

        if (article is null ||
            string.IsNullOrWhiteSpace(article.Title) ||
            string.IsNullOrWhiteSpace(article.ArticleHtml) ||
            string.IsNullOrWhiteSpace(article.InstagramCaption))
        {
            throw new InvalidOperationException(
                $"{providerName} article response must contain title, articleHtml, " +
                "and instagramCaption.");
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
}
