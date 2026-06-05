using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace ContentProducer.Worker;

public sealed class SourcePageImagesProvider : IImageProvider
{
    private static readonly Regex MetaImagePattern = new(
        "<meta[^>]+(?:property|name)=[\"'](?:og:image|twitter:image)[\"'][^>]+content=[\"'](?<url>[^\"']+)[\"'][^>]*>|" +
        "<meta[^>]+content=[\"'](?<url>[^\"']+)[\"'][^>]+(?:property|name)=[\"'](?:og:image|twitter:image)[\"'][^>]*>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex ImageTagPattern = new(
        "<img[^>]+(?:src|data-src)=[\"'](?<url>[^\"']+)[\"'][^>]*>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly HttpClient HttpClient = CreateHttpClient();

    private readonly ILogger<SourcePageImagesProvider> _logger;
    private readonly SourcePageImageOptions _options;

    public SourcePageImagesProvider(
        IOptions<SourcePageImageOptions> options,
        ILogger<SourcePageImagesProvider> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public string Name => ProviderNames.SourcePageImages;

    public async Task<GeneratedImage?> GenerateImageAsync(
        GeneratedArticle article,
        CancellationToken cancellationToken)
    {
        Uri[] sourceUris = GetSourceUris(article);

        if (sourceUris.Length == 0)
        {
            throw new InvalidOperationException(
                "SourcePageImages provider needs article references with valid URLs.");
        }

        foreach (Uri sourceUri in sourceUris)
        {
            GeneratedImage? image = await TryGetImageFromSourceAsync(
                sourceUri,
                cancellationToken);

            if (image is not null)
            {
                return image;
            }
        }

        throw new InvalidOperationException(
            "Could not find a usable image in the article source pages.");
    }

    private async Task<GeneratedImage?> TryGetImageFromSourceAsync(
        Uri sourceUri,
        CancellationToken cancellationToken)
    {
        string html;

        try
        {
            html = await HttpClient.GetStringAsync(sourceUri, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(
                exception,
                "Skipping source page {SourceUrl}. Could not download page.",
                sourceUri);

            return null;
        }

        Uri[] imageUris = ExtractImageUris(sourceUri, html);

        _logger.LogInformation(
            "Found {ImageCandidateCount} image candidates in source page {SourceUrl}.",
            imageUris.Length,
            sourceUri);

        foreach (Uri imageUri in imageUris.Take(
            Math.Max(1, _options.MaxImageCandidatesPerPage)))
        {
            GeneratedImage? image = await TryDownloadImageAsync(
                imageUri,
                sourceUri,
                cancellationToken);

            if (image is not null)
            {
                return image;
            }
        }

        return null;
    }

    private Uri[] ExtractImageUris(Uri sourceUri, string html)
    {
        IEnumerable<string> metaImages = MetaImagePattern
            .Matches(html)
            .Select(match => WebUtility.HtmlDecode(match.Groups["url"].Value));
        IEnumerable<string> imageTags = ImageTagPattern
            .Matches(html)
            .Select(match => WebUtility.HtmlDecode(match.Groups["url"].Value));

        IEnumerable<string> candidates = _options.PreferOpenGraphImages
            ? metaImages.Concat(imageTags)
            : imageTags.Concat(metaImages);

        return candidates
            .Select(candidate => ToAbsoluteUri(sourceUri, candidate))
            .Where(uri => uri is not null)
            .Cast<Uri>()
            .Where(IsAllowedImageUri)
            .DistinctBy(uri => uri.AbsoluteUri)
            .ToArray();
    }

    private async Task<GeneratedImage?> TryDownloadImageAsync(
        Uri imageUri,
        Uri sourceUri,
        CancellationToken cancellationToken)
    {
        try
        {
            using HttpResponseMessage response = await HttpClient.GetAsync(
                imageUri,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            string contentType = response.Content.Headers.ContentType?.MediaType
                ?? "application/octet-stream";

            if (!contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            byte[] content = await response.Content.ReadAsByteArrayAsync(
                cancellationToken);

            if (content.Length < _options.MinImageBytes ||
                content.Length > _options.MaxImageBytes)
            {
                return null;
            }

            _logger.LogInformation(
                "Selected source page image {ImageUrl} from {SourceUrl}. Bytes: {ImageBytes}.",
                imageUri,
                sourceUri,
                content.Length);

            return new GeneratedImage(
                content,
                contentType,
                GetFileName(contentType, imageUri),
                $"Image source: {sourceUri.Host} - {sourceUri}",
                imageUri.AbsoluteUri);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    private Uri[] GetSourceUris(GeneratedArticle article)
    {
        return (article.References ?? Array.Empty<GeneratedReference>())
            .Select(reference => reference.Url)
            .Where(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .Select(url => new Uri(url))
            .Where(IsAllowedSourceUri)
            .Take(Math.Max(1, _options.MaxSourcePages))
            .ToArray();
    }

    private bool IsAllowedSourceUri(Uri uri)
    {
        if (uri.Scheme is not ("http" or "https"))
        {
            return false;
        }

        string host = uri.Host.ToLowerInvariant();

        if (_options.BlockedHosts.Any(blocked =>
                host.EndsWith(blocked.ToLowerInvariant(), StringComparison.Ordinal)))
        {
            return false;
        }

        return _options.AllowedHosts.Length == 0 ||
            _options.AllowedHosts.Any(allowed =>
                host.EndsWith(allowed.ToLowerInvariant(), StringComparison.Ordinal));
    }

    private bool IsAllowedImageUri(Uri uri)
    {
        return uri.Scheme is "http" or "https" &&
            !uri.AbsolutePath.EndsWith(".svg", StringComparison.OrdinalIgnoreCase);
    }

    private static Uri? ToAbsoluteUri(Uri sourceUri, string candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate) ||
            candidate.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return Uri.TryCreate(candidate, UriKind.Absolute, out Uri? absolute)
            ? absolute
            : Uri.TryCreate(sourceUri, candidate, out Uri? relative)
                ? relative
                : null;
    }

    private static string GetFileName(string contentType, Uri imageUri)
    {
        string extension = contentType.ToLowerInvariant() switch
        {
            "image/png" => ".png",
            "image/webp" => ".webp",
            "image/gif" => ".gif",
            _ => ".jpg"
        };

        string baseName = Path.GetFileNameWithoutExtension(imageUri.LocalPath);

        return string.IsNullOrWhiteSpace(baseName)
            ? "source-page-image" + extension
            : baseName + extension;
    }

    private static HttpClient CreateHttpClient()
    {
        HttpClient client = new()
        {
            Timeout = TimeSpan.FromSeconds(45)
        };

        client.DefaultRequestHeaders.UserAgent.ParseAdd("ContentProducer/1.0");
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("text/html"));
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("image/*"));

        return client;
    }
}
