using System.Net;
using System.Text.RegularExpressions;

namespace ContentProducer.Worker;

public static class WordPressContentFormatter
{
    private static readonly Regex ContentReferencePattern = new(
        @"\s*:contentReference\[oaicite:\d+\]\{index=\d+\}",
        RegexOptions.Compiled);

    public static string BuildPostContent(
        GeneratedArticle article,
        WordPressMedia? featuredImage,
        IReadOnlyList<string> internalLinkUrls)
    {
        string articleHtml = RemoveContentReferences(article.ArticleHtml);
        string referencesHtml = BuildReferences(article.References);
        string internalLinksHtml = BuildInternalLinks(internalLinkUrls);
        string imageHtml = BuildImage(featuredImage, article);

        return string.Join(
            Environment.NewLine,
            imageHtml,
            articleHtml,
            referencesHtml,
            internalLinksHtml);
    }

    public static string RemoveContentReferences(string html)
    {
        return ContentReferencePattern.Replace(html, string.Empty);
    }

    public static string GetFocusKeyphrase(GeneratedArticle article)
    {
        return string.IsNullOrWhiteSpace(article.FocusKeyphrase)
            ? article.Title
            : article.FocusKeyphrase;
    }

    private static string BuildImage(
        WordPressMedia? featuredImage,
        GeneratedArticle article)
    {
        if (featuredImage is null)
        {
            return string.Empty;
        }

        string altText = WebUtility.HtmlEncode(GetFocusKeyphrase(article));
        string imageUrl = WebUtility.HtmlEncode(featuredImage.SourceUrl);

        return $"<figure class=\"wp-block-image\"><img src=\"{imageUrl}\" alt=\"{altText}\" /></figure>";
    }

    private static string BuildInternalLinks(IReadOnlyList<string> internalLinkUrls)
    {
        string[] urls = internalLinkUrls
            .Where(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (urls.Length == 0)
        {
            return string.Empty;
        }

        string items = string.Join(
            Environment.NewLine,
            urls.Select(url =>
                $"<li><a href=\"{WebUtility.HtmlEncode(url)}\">مطالب مرتبط</a></li>"));

        return $"<h2>مطالب مرتبط</h2><ul>{items}</ul>";
    }

    private static string BuildReferences(IReadOnlyList<GeneratedReference>? references)
    {
        if (references is null || references.Count == 0)
        {
            return string.Empty;
        }

        string[] items = references
            .Where(reference =>
                !string.IsNullOrWhiteSpace(reference.Title) &&
                Uri.TryCreate(reference.Url, UriKind.Absolute, out _))
            .Select(reference =>
                $"<li><a href=\"{WebUtility.HtmlEncode(reference.Url)}\" " +
                $"rel=\"noopener noreferrer\">{WebUtility.HtmlEncode(reference.Title)}</a></li>")
            .ToArray();

        return items.Length == 0
            ? string.Empty
            : $"<h2>منابع</h2><ul>{string.Join(Environment.NewLine, items)}</ul>";
    }
}
