namespace ContentProducer.Worker;

public sealed record WordPressMedia(
    int Id,
    string SourceUrl,
    string? Caption = null);
