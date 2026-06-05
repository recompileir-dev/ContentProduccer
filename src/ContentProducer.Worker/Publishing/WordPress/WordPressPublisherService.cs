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

    public async Task ValidateConnectionAsync(CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await _httpClient.GetAsync(
            "wp-json/wp/v2/posts?context=edit&per_page=1",
            cancellationToken);
        string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, responseBody, "validate WordPress REST API authentication");

        _logger.LogInformation("WordPress REST API authentication is valid.");

        await ValidateCategoryAsync(cancellationToken);
    }

    public async Task<WordPressMedia> UploadImageAsync(
        GeneratedArticle article,
        GeneratedImage image,
        CancellationToken cancellationToken)
    {
        const string fileName = "article-image.png";
        using HttpRequestMessage request = new(HttpMethod.Post, "wp-json/wp/v2/media");

        ByteArrayContent content = new(image.Content);
        content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
        {
            FileName = fileName
        };
        request.Content = content;

        using HttpResponseMessage response =
            await _httpClient.SendAsync(request, cancellationToken);
        string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, responseBody, "upload WordPress media");

        using JsonDocument document = JsonDocument.Parse(responseBody);
        int id = document.RootElement.GetProperty("id").GetInt32();
        string sourceUrl = document.RootElement.GetProperty("source_url").GetString()
            ?? throw new InvalidOperationException("WordPress media response has no source_url.");

        await UpdateMediaMetadataAsync(id, article, cancellationToken);

        _logger.LogInformation("Uploaded WordPress article image {MediaId}.", id);

        return new WordPressMedia(id, sourceUrl);
    }

    public async Task<WordPressPost> PublishPostAsync(
        GeneratedArticle article,
        WordPressMedia? featuredImage,
        CancellationToken cancellationToken)
    {
        string postContent = WordPressContentFormatter.BuildPostContent(
            article,
            featuredImage,
            _options.InternalLinkUrls);
        Dictionary<string, string>? seoMeta = BuildSeoMeta(article);

        Dictionary<string, object?> post = new()
        {
            ["title"] = article.Title,
            ["content"] = postContent,
            ["excerpt"] = article.MetaDescription,
            ["status"] = _options.PostStatus
        };

        if (featuredImage is not null)
        {
            post["featured_media"] = featuredImage.Id;
        }

        if (_options.CategoryId.HasValue)
        {
            post["categories"] = new[] { _options.CategoryId.Value };
        }

        if (seoMeta is not null)
        {
            post["meta"] = seoMeta;
        }

        string json = JsonSerializer.Serialize(post, JsonOptions);

        using HttpRequestMessage request = new(HttpMethod.Post, "wp-json/wp/v2/posts")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        using HttpResponseMessage response =
            await _httpClient.SendAsync(request, cancellationToken);
        string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, responseBody, "create WordPress post");

        using JsonDocument document = JsonDocument.Parse(responseBody);
        int postId = document.RootElement.GetProperty("id").GetInt32();
        string postLink = document.RootElement.GetProperty("link").GetString()
            ?? throw new InvalidOperationException("WordPress post response has no link.");

        EnsureCategoryAssigned(document.RootElement, postId);

        _logger.LogInformation(
            "Created WordPress post {PostId} with status {PostStatus}, title {Title}, " +
            "and category {CategoryId}.",
            postId,
            _options.PostStatus,
            article.Title,
            _options.CategoryId);

        return new WordPressPost(postId, postLink);
    }

    private async Task ValidateCategoryAsync(CancellationToken cancellationToken)
    {
        if (!_options.CategoryId.HasValue)
        {
            if (_options.RequireCategoryId)
            {
                throw new InvalidOperationException(
                    "WordPress category is required. Set WordPress:CategoryId or " +
                    "the WORDPRESS_CATEGORY_ID environment variable.");
            }

            return;
        }

        if (_options.CategoryId.Value <= 0)
        {
            throw new InvalidOperationException(
                "WordPress:CategoryId must be a positive numeric category ID.");
        }

        using HttpResponseMessage response = await _httpClient.GetAsync(
            $"wp-json/wp/v2/categories/{_options.CategoryId.Value}?context=view",
            cancellationToken);
        string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(
            response,
            responseBody,
            $"validate WordPress category {_options.CategoryId.Value}");

        using JsonDocument document = JsonDocument.Parse(responseBody);
        string categoryName = document.RootElement.GetProperty("name").GetString()
            ?? _options.CategoryId.Value.ToString();

        _logger.LogInformation(
            "WordPress category {CategoryId} ({CategoryName}) is valid.",
            _options.CategoryId.Value,
            categoryName);
    }

    private void EnsureCategoryAssigned(JsonElement post, int postId)
    {
        if (!_options.CategoryId.HasValue)
        {
            return;
        }

        bool categoryAssigned =
            post.TryGetProperty("categories", out JsonElement categories) &&
            categories.EnumerateArray().Any(category =>
                category.GetInt32() == _options.CategoryId.Value);

        if (!categoryAssigned)
        {
            throw new InvalidOperationException(
                $"WordPress post {postId} was created without the configured category " +
                $"{_options.CategoryId.Value}.");
        }
    }

    private async Task UpdateMediaMetadataAsync(
        int mediaId,
        GeneratedArticle article,
        CancellationToken cancellationToken)
    {
        string altText = WordPressContentFormatter.GetFocusKeyphrase(article);
        string json = JsonSerializer.Serialize(
            new
            {
                alt_text = altText,
                title = article.Title,
                caption = article.Title
            },
            JsonOptions);

        using HttpRequestMessage request = new(
            HttpMethod.Post,
            $"wp-json/wp/v2/media/{mediaId}")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        using HttpResponseMessage response =
            await _httpClient.SendAsync(request, cancellationToken);
        string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, responseBody, "update WordPress media metadata");
    }

    private Dictionary<string, string>? BuildSeoMeta(GeneratedArticle article)
    {
        if (!_options.SendSeoMeta)
        {
            return null;
        }

        return new Dictionary<string, string>
        {
            [_options.FocusKeyphraseMetaKey] =
                WordPressContentFormatter.GetFocusKeyphrase(article),
            [_options.SeoTitleMetaKey] =
                string.IsNullOrWhiteSpace(article.SeoTitle) ? article.Title : article.SeoTitle,
            [_options.MetaDescriptionMetaKey] =
                article.MetaDescription ?? string.Empty
        };
    }

    private static HttpClient CreateHttpClient(WordPressOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.SiteUrl))
        {
            throw new InvalidOperationException(
                "WordPress:SiteUrl is required.");
        }

        string? username = Environment.GetEnvironmentVariable(
            options.UsernameEnvironmentVariable);

        if (string.IsNullOrWhiteSpace(username))
        {
            throw new InvalidOperationException(
                "WordPress username is missing. Set the " +
                $"'{options.UsernameEnvironmentVariable}' environment variable.");
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

        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.UserAgent.ParseAdd("ContentProducer/1.0");

        string credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{username.Trim()}:{password}"));
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
            string contentType = response.Content.Headers.ContentType?.MediaType ?? "unknown";
            string responseSummary = SummarizeResponseBody(responseBody);

            throw new InvalidOperationException(
                $"Failed to {operation}. Status {(int)response.StatusCode}, " +
                $"content type '{contentType}'. Response: {responseSummary}");
        }
    }

    private static string SummarizeResponseBody(string responseBody)
    {
        const int maxLength = 800;

        string summary = responseBody
            .Replace("\r", " ")
            .Replace("\n", " ")
            .Trim();

        while (summary.Contains("  ", StringComparison.Ordinal))
        {
            summary = summary.Replace("  ", " ");
        }

        if (summary.Length <= maxLength)
        {
            return summary;
        }

        return summary.Substring(0, maxLength) + "...";
    }
}
