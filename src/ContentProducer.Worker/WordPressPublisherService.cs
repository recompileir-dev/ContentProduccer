using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace ContentProducer.Worker;

public sealed class WordPressPublisherService : IWordPressPublisherService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly ILogger<WordPressPublisherService> _logger;
    private readonly WordPressOptions _options;

    public WordPressPublisherService(
        IOptions<WordPressOptions> options,
        ILogger<WordPressPublisherService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _httpClient = CreateHttpClient(_options);
    }

    public async Task<IReadOnlyList<WordPressMedia>> UploadImagesAsync(
        GeneratedArticle article,
        IReadOnlyList<GeneratedImage> images,
        CancellationToken cancellationToken)
    {
        List<WordPressMedia> mediaItems = new(images.Count);

        foreach (GeneratedImage image in images)
        {
            string fileName = $"carousel-{image.SlideNumber:00}.png";
            using HttpRequestMessage request = new(HttpMethod.Post, "wp-json/wp/v2/media");
            request.Headers.TryAddWithoutValidation(
                "Content-Disposition",
                $"attachment; filename=\"{fileName}\"");

            ByteArrayContent content = new(image.Content);
            content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            request.Content = content;

            using HttpResponseMessage response =
                await _httpClient.SendAsync(request, cancellationToken);
            string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            EnsureSuccess(response, responseBody, "upload WordPress media");

            using JsonDocument document = JsonDocument.Parse(responseBody);
            int id = document.RootElement.GetProperty("id").GetInt32();
            string sourceUrl = document.RootElement.GetProperty("source_url").GetString()
                ?? throw new InvalidOperationException("WordPress media response has no source_url.");

            mediaItems.Add(new WordPressMedia(id, sourceUrl));
            _logger.LogInformation(
                "Uploaded WordPress media {MediaId} for carousel slide {SlideNumber}.",
                id,
                image.SlideNumber);
        }

        return mediaItems;
    }

    public async Task PublishPostAsync(
        GeneratedArticle article,
        WordPressMedia featuredImage,
        CancellationToken cancellationToken)
    {
        string json = JsonSerializer.Serialize(
            new
            {
                title = article.Title,
                content = article.ArticleHtml,
                status = _options.PostStatus,
                featured_media = featuredImage.Id
            },
            JsonOptions);

        using HttpRequestMessage request = new(HttpMethod.Post, "wp-json/wp/v2/posts")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        using HttpResponseMessage response =
            await _httpClient.SendAsync(request, cancellationToken);
        string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, responseBody, "publish WordPress post");

        _logger.LogInformation("Published WordPress post with title {Title}.", article.Title);
    }

    private static HttpClient CreateHttpClient(WordPressOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.SiteUrl) ||
            string.IsNullOrWhiteSpace(options.Username))
        {
            throw new InvalidOperationException(
                "WordPress:SiteUrl and WordPress:Username are required.");
        }

        string? password = options.ApplicationPassword;

        if (string.IsNullOrWhiteSpace(password))
        {
            password = Environment.GetEnvironmentVariable(
                options.ApplicationPasswordEnvironmentVariable);
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "WordPress application password is missing. Set WordPress:ApplicationPassword " +
                $"or '{options.ApplicationPasswordEnvironmentVariable}'.");
        }

        HttpClient client = new()
        {
            BaseAddress = new Uri(options.SiteUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromMinutes(5)
        };

        string credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{options.Username}:{password}"));
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", credentials);

        return client;
    }

    private static void EnsureSuccess(
        HttpResponseMessage response,
        string responseBody,
        string operation)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Failed to {operation}. Status {(int)response.StatusCode}: {responseBody}");
        }
    }
}
