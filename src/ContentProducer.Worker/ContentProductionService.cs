namespace ContentProducer.Worker;

public sealed class ContentProductionService : IContentProductionService
{
    private readonly IOpenAiArticleService _articleService;
    private readonly IOpenAiImageService _imageService;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILogger<ContentProductionService> _logger;
    private readonly OpenAiOptions _options;

    public ContentProductionService(
        IOpenAiArticleService articleService,
        IOpenAiImageService imageService,
        IHostEnvironment hostEnvironment,
        Microsoft.Extensions.Options.IOptions<OpenAiOptions> options,
        ILogger<ContentProductionService> logger)
    {
        _articleService = articleService;
        _imageService = imageService;
        _hostEnvironment = hostEnvironment;
        _logger = logger;
        _options = options.Value;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Content production service started at {StartedAt}.",
            DateTimeOffset.Now);

        string promptPath = ResolvePath(_options.PromptFilePath);
        string outputDirectory = ResolvePath(_options.OutputDirectory);
        string runDirectory = Path.Combine(
            outputDirectory,
            DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss"));

        string prompt = await File.ReadAllTextAsync(promptPath, cancellationToken);
        string article = await _articleService.GenerateArticleAsync(prompt, cancellationToken);
        IReadOnlyList<GeneratedImage> images =
            await _imageService.GenerateCarouselImagesAsync(article, cancellationToken);

        Directory.CreateDirectory(runDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(runDirectory, "article.md"),
            article,
            cancellationToken);

        foreach (GeneratedImage image in images)
        {
            string imagePath = Path.Combine(
                runDirectory,
                $"carousel-{image.SlideNumber:00}.png");

            await File.WriteAllBytesAsync(imagePath, image.Content, cancellationToken);
        }

        _logger.LogInformation(
            "Content production service completed at {CompletedAt}. Output: {OutputDirectory}.",
            DateTimeOffset.Now,
            runDirectory);
    }

    private string ResolvePath(string path)
    {
        return Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(path, _hostEnvironment.ContentRootPath);
    }
}
