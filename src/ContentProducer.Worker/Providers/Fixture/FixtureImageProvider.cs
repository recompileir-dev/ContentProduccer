using Microsoft.Extensions.Options;

namespace ContentProducer.Worker;

public sealed class FixtureImageProvider : IImageProvider
{
    private readonly FixtureOptions _options;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILogger<FixtureImageProvider> _logger;

    public FixtureImageProvider(
        IOptions<FixtureOptions> options,
        IHostEnvironment hostEnvironment,
        ILogger<FixtureImageProvider> logger)
    {
        _options = options.Value;
        _hostEnvironment = hostEnvironment;
        _logger = logger;
    }

    public string Name => ProviderNames.Fixture;

    public async Task<GeneratedImage?> GenerateImageAsync(
        GeneratedArticle article,
        CancellationToken cancellationToken)
    {
        string imagePath = ResolvePath(_options.ImageFilePath);

        if (!File.Exists(imagePath))
        {
            throw new InvalidOperationException(
                $"Fixture image file '{imagePath}' does not exist.");
        }

        byte[] imageContent = await File.ReadAllBytesAsync(
            imagePath,
            cancellationToken);

        _logger.LogInformation(
            "Loaded fixture image from {ImagePath}.",
            imagePath);

        return new GeneratedImage(
            imageContent,
            GetContentType(imagePath),
            Path.GetFileName(imagePath));
    }

    private string ResolvePath(string path)
    {
        return Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(path, _hostEnvironment.ContentRootPath);
    }

    private static string GetContentType(string imagePath)
    {
        return Path.GetExtension(imagePath).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            _ => "image/png"
        };
    }
}
