namespace ContentProducer.Worker;

public sealed class PublishingOptions
{
    public const string SectionName = "Publishing";

    public bool PublishToWordPress { get; init; } = true;

    public bool PublishToInstagram { get; init; } = true;
}
