namespace ContentProducer.Worker;

public sealed record GeneratedArticle(
    string Title,
    string ArticleHtml,
    string InstagramCaption,
    string? FocusKeyphrase = null,
    string? SeoTitle = null,
    string? MetaDescription = null,
    IReadOnlyList<GeneratedReference>? References = null);
