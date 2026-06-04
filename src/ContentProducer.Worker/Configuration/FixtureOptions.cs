namespace ContentProducer.Worker;

public sealed class FixtureOptions
{
    public const string SectionName = "Fixture";

    public string ArticleFilePath { get; init; } = "fixtures/article-example.json";

    public string ImageFilePath { get; init; } = "fixtures/images/article-image.png";
}
