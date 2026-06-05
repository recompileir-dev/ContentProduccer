using Microsoft.Extensions.Options;

namespace ContentProducer.Worker;

public sealed class ContentGeneratorService : IContentGeneratorService
{
    private readonly ContentGenerationOptions _options;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IReadOnlyList<IImageProvider> _imageProviders;
    private readonly IReadOnlyList<ILlmProvider> _llmProviders;
    private readonly ILogger<ContentGeneratorService> _logger;

    public ContentGeneratorService(
        IEnumerable<ILlmProvider> llmProviders,
        IEnumerable<IImageProvider> imageProviders,
        IHostEnvironment hostEnvironment,
        IOptions<ContentGenerationOptions> options,
        ILogger<ContentGeneratorService> logger)
    {
        _llmProviders = llmProviders.ToArray();
        _imageProviders = imageProviders.ToArray();
        _hostEnvironment = hostEnvironment;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<GeneratedContent> GenerateAsync(
        CancellationToken cancellationToken)
    {
        ILlmProvider llmProvider = ResolveProvider(
            _llmProviders,
            _options.LlmProvider,
            "LLM");
        IImageProvider imageProvider = ResolveProvider(
            _imageProviders,
            _options.ImageProvider,
            "image");

        _logger.LogInformation(
            "Generating content with LLM provider {LlmProvider} and image provider {ImageProvider}.",
            llmProvider.Name,
            imageProvider.Name);

        string promptPath = ResolvePath(GetPromptFilePath(llmProvider.Name));
        _logger.LogInformation(
            "Loading article prompt for provider {LlmProvider} from {PromptPath}.",
            llmProvider.Name,
            promptPath);
        string prompt = await File.ReadAllTextAsync(promptPath, cancellationToken);
        GeneratedArticle article =
            await llmProvider.GenerateArticleAsync(prompt, cancellationToken);
        GeneratedImage? image = await TryGenerateImageAsync(
            imageProvider,
            article,
            cancellationToken);

        return new GeneratedContent(NormalizeArticle(article), image);
    }

    private async Task<GeneratedImage?> TryGenerateImageAsync(
        IImageProvider imageProvider,
        GeneratedArticle article,
        CancellationToken cancellationToken)
    {
        try
        {
            return await imageProvider.GenerateImageAsync(article, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Image provider {ImageProvider} failed. Continuing without an image.",
                imageProvider.Name);

            return null;
        }
    }

    private static TProvider ResolveProvider<TProvider>(
        IReadOnlyList<TProvider> providers,
        string providerName,
        string providerType)
        where TProvider : class
    {
        TProvider? provider = providers.FirstOrDefault(item =>
        {
            string? name = item switch
            {
                ILlmProvider llmProvider => llmProvider.Name,
                IImageProvider imageProvider => imageProvider.Name,
                _ => null
            };

            return string.Equals(name, providerName, StringComparison.OrdinalIgnoreCase);
        });

        if (provider is not null)
        {
            return provider;
        }

        string availableProviders = string.Join(
            ", ",
            providers.Select(item => item switch
            {
                ILlmProvider llmProvider => llmProvider.Name,
                IImageProvider imageProvider => imageProvider.Name,
                _ => item.GetType().Name
            }));

        throw new InvalidOperationException(
            $"Unknown {providerType} provider '{providerName}'. Available providers: " +
            availableProviders);
    }

    private static GeneratedArticle NormalizeArticle(GeneratedArticle article)
    {
        string focusKeyphrase = string.IsNullOrWhiteSpace(article.FocusKeyphrase)
            ? article.Title
            : article.FocusKeyphrase;
        string seoTitle = string.IsNullOrWhiteSpace(article.SeoTitle)
            ? article.Title
            : article.SeoTitle;
        string metaDescription = string.IsNullOrWhiteSpace(article.MetaDescription)
            ? TrimToLength(article.InstagramCaption, 155)
            : article.MetaDescription;

        return article with
        {
            FocusKeyphrase = focusKeyphrase,
            SeoTitle = seoTitle,
            MetaDescription = metaDescription,
            References = article.References ?? Array.Empty<GeneratedReference>()
        };
    }

    private static string TrimToLength(string value, int maxLength)
    {
        if (value.Length <= maxLength)
        {
            return value;
        }

        return value.Substring(0, maxLength - 3) + "...";
    }

    private string ResolvePath(string path)
    {
        return Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(path, _hostEnvironment.ContentRootPath);
    }

    private string GetPromptFilePath(string providerName)
    {
        KeyValuePair<string, string> providerPrompt = _options.ProviderPromptFilePaths
            .FirstOrDefault(item =>
                string.Equals(item.Key, providerName, StringComparison.OrdinalIgnoreCase));

        return string.IsNullOrWhiteSpace(providerPrompt.Value)
            ? _options.PromptFilePath
            : providerPrompt.Value;
    }
}
