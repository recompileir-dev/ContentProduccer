namespace ContentProducer.Worker;

public sealed class ContentSourceOptions
{
    public const string SectionName = "ContentSource";

    public bool UseFixture { get; init; }

    public string FixtureArticleFilePath { get; init; } = "fixtures/article-example.json";

    public string FixtureImageFilePath { get; init; } = "fixtures/images/article-image.png";
}
