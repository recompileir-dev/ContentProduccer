namespace ContentProducer.Worker;

public sealed record GeneratedContent(
    GeneratedArticle Article,
    GeneratedImage? Image);
