namespace ContentProducer.Worker;

public sealed class ContentSourceOptions
{
    public const string SectionName = "ContentSource";

    public bool UseFixture { get; init; }

    public string FixtureArticleFilePath { get; init; } = "fixtures/article-example.json";

    public string FixtureImagesDirectory { get; init; } = "fixtures/images";
}
