namespace ContentProducer.Worker;

public sealed record GeneratedImage(
    byte[] Content,
    string ContentType = "image/png",
    string FileName = "article-image.png");
