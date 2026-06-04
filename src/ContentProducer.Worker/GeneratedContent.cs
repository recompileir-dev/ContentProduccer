namespace ContentProducer.Worker;

public sealed record GeneratedContent(
    GeneratedArticle Article,
    IReadOnlyList<GeneratedImage> Images);
