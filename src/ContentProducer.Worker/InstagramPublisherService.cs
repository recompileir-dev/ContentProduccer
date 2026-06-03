using System.Text.Json;
using Microsoft.Extensions.Options;

namespace ContentProducer.Worker;

public sealed class InstagramPublisherService : IInstagramPublisherService
{
    private readonly HttpClient _httpClient;
    private readonly InstagramOptions _options;
    private readonly ILogger<InstagramPublisherService> _logger;
    private readonly string _accessToken;

    public InstagramPublisherService(
        IOptions<InstagramOptions> options,
        ILogger<InstagramPublisherService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _accessToken = GetAccessToken(_options);
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_options.GraphApiBaseUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromMinutes(5)
        };
    }

    public async Task PublishCarouselAsync(
        string caption,
        IReadOnlyList<string> imageUrls,
        CancellationToken cancellationToken)
    {
        if (imageUrls.Count < 2 || imageUrls.Count > 10)
        {
            throw new InvalidOperationException(
                "Instagram carousel publishing requires between 2 and 10 images.");
        }

        List<string> childContainerIds = new(imageUrls.Count);

        foreach (string imageUrl in imageUrls)
        {
            string childId = await CreateMediaContainerAsync(
                new Dictionary<string, string>
                {
                    ["image_url"] = imageUrl,
                    ["is_carousel_item"] = "true"
                },
                cancellationToken);

            childContainerIds.Add(childId);
            await WaitForContainerAsync(childId, cancellationToken);
        }

        string carouselId = await CreateMediaContainerAsync(
            new Dictionary<string, string>
            {
                ["media_type"] = "CAROUSEL",
                ["caption"] = caption,
                ["children"] = string.Join(",", childContainerIds)
            },
            cancellationToken);

        await WaitForContainerAsync(carouselId, cancellationToken);

        await PostFormAsync(
            $"{ApiPath()}/media_publish",
            new Dictionary<string, string>
            {
                ["creation_id"] = carouselId,
                ["access_token"] = _accessToken
            },
            cancellationToken);

        _logger.LogInformation(
            "Published Instagram carousel with {ImageCount} images.",
            imageUrls.Count);
    }

    private async Task<string> CreateMediaContainerAsync(
        Dictionary<string, string> values,
        CancellationToken cancellationToken)
    {
        values["access_token"] = _accessToken;
        using JsonDocument response = await PostFormAsync(
            $"{ApiPath()}/media",
            values,
            cancellationToken);

        return response.RootElement.GetProperty("id").GetString()
            ?? throw new InvalidOperationException("Instagram media response has no id.");
    }

    private async Task<JsonDocument> PostFormAsync(
        string relativeUrl,
        Dictionary<string, string> values,
        CancellationToken cancellationToken)
    {
        using FormUrlEncodedContent content = new(values);
        using HttpResponseMessage response =
            await _httpClient.PostAsync(relativeUrl, content, cancellationToken);
        string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Instagram API request failed with status {(int)response.StatusCode}: " +
                responseBody);
        }

        return JsonDocument.Parse(responseBody);
    }

    private async Task WaitForContainerAsync(
        string containerId,
        CancellationToken cancellationToken)
    {
        for (int attempt = 1; attempt <= 10; attempt++)
        {
            string relativeUrl =
                $"{_options.GraphApiVersion.Trim('/')}/{containerId}" +
                $"?fields=status_code&access_token={Uri.EscapeDataString(_accessToken)}";

            using HttpResponseMessage response =
                await _httpClient.GetAsync(relativeUrl, cancellationToken);
            string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Instagram container status request failed with status " +
                    $"{(int)response.StatusCode}: {responseBody}");
            }

            using JsonDocument document = JsonDocument.Parse(responseBody);
            string? status = document.RootElement.GetProperty("status_code").GetString();

            if (status == "FINISHED")
            {
                return;
            }

            if (status == "ERROR" || status == "EXPIRED")
            {
                throw new InvalidOperationException(
                    $"Instagram media container {containerId} has status {status}.");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }

        throw new InvalidOperationException(
            $"Instagram media container {containerId} was not ready in time.");
    }

    private string ApiPath()
    {
        if (string.IsNullOrWhiteSpace(_options.InstagramUserId) ||
            string.IsNullOrWhiteSpace(_options.GraphApiVersion))
        {
            throw new InvalidOperationException(
                "Instagram:InstagramUserId and Instagram:GraphApiVersion are required.");
        }

        return $"{_options.GraphApiVersion.Trim('/')}/{_options.InstagramUserId}";
    }

    private static string GetAccessToken(InstagramOptions options)
    {
        string? accessToken = options.AccessToken;

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            accessToken = Environment.GetEnvironmentVariable(
                options.AccessTokenEnvironmentVariable);
        }

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new InvalidOperationException(
                "Instagram access token is missing. Set Instagram:AccessToken or " +
                $"'{options.AccessTokenEnvironmentVariable}'.");
        }

        return accessToken;
    }
}
