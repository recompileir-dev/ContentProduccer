using System.Text.Json;
using Microsoft.Extensions.Options;

namespace ContentProducer.Worker;

public sealed class InstagramPublisherService : IInstagramPublisherService
{
    private readonly HttpClient _httpClient;
    private readonly InstagramOptions _options;
    private readonly ILogger<InstagramPublisherService> _logger;

    public InstagramPublisherService(
        IOptions<InstagramOptions> options,
        ILogger<InstagramPublisherService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_options.GraphApiBaseUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromMinutes(5)
        };
    }

    public async Task PublishPostAsync(
        string caption,
        string imageUrl,
        CancellationToken cancellationToken)
    {
        string accessToken = GetAccessToken(_options);
        string containerId = await CreateMediaContainerAsync(
            accessToken,
            imageUrl,
            caption,
            cancellationToken);

        await WaitForContainerAsync(containerId, accessToken, cancellationToken);

        await PostFormAsync(
            $"{ApiPath()}/media_publish",
            new Dictionary<string, string>
            {
                ["creation_id"] = containerId,
                ["access_token"] = accessToken
            },
            cancellationToken);

        _logger.LogInformation("Published Instagram image post.");
    }

    private async Task<string> CreateMediaContainerAsync(
        string accessToken,
        string imageUrl,
        string caption,
        CancellationToken cancellationToken)
    {
        using JsonDocument response = await PostFormAsync(
            $"{ApiPath()}/media",
            new Dictionary<string, string>
            {
                ["image_url"] = imageUrl,
                ["caption"] = caption,
                ["access_token"] = accessToken
            },
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
        string accessToken,
        CancellationToken cancellationToken)
    {
        for (int attempt = 1; attempt <= 10; attempt++)
        {
            string relativeUrl =
                $"{_options.GraphApiVersion.Trim('/')}/{containerId}" +
                $"?fields=status_code&access_token={Uri.EscapeDataString(accessToken)}";

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
