namespace ContentProducer.Worker;

public sealed class SourcePageImageOptions
{
    public const string SectionName = "SourcePageImages";

    public int MaxSourcePages { get; init; } = 5;

    public int MaxImageCandidatesPerPage { get; init; } = 6;

    public int MaxImageBytes { get; init; } = 8_000_000;

    public int MinImageBytes { get; init; } = 15_000;

    public bool PreferOpenGraphImages { get; init; } = true;

    public string[] AllowedHosts { get; init; } = Array.Empty<string>();

    public string[] BlockedHosts { get; init; } = Array.Empty<string>();
}
