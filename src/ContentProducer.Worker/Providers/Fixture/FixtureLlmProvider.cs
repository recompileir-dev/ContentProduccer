using Microsoft.Extensions.Options;

namespace ContentProducer.Worker;

public sealed class FixtureLlmProvider : ILlmProvider
{
    private readonly FixtureOptions _options;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILogger<FixtureLlmProvider> _logger;

    public FixtureLlmProvider(
        IOptions<FixtureOptions> options,
        IHostEnvironment hostEnvironment,
        ILogger<FixtureLlmProvider> logger)
    {
        _options = options.Value;
        _hostEnvironment = hostEnvironment;
        _logger = logger;
    }

    public string Name => ProviderNames.Fixture;

    public async Task<GeneratedArticle> GenerateArticleAsync(
        string prompt,
        CancellationToken cancellationToken)
    {
        string articlePath = ResolvePath(_options.ArticleFilePath);
        string articleJson = await File.ReadAllTextAsync(
            articlePath,
            cancellationToken);

        _logger.LogInformation(
            "Loaded fixture article from {ArticlePath}.",
            articlePath);

        return GeneratedArticleParser.Parse(articleJson, Name);
    }

    private string ResolvePath(string path)
    {
        return Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(path, _hostEnvironment.ContentRootPath);
    }
}
