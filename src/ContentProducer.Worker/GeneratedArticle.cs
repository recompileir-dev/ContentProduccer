namespace ContentProducer.Worker;

public sealed record GeneratedArticle(
    string Title,
    string ArticleHtml,
    string InstagramCaption);
