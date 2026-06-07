using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace ContentProducer.Worker;

public sealed class OpenApiRouterApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly OpenApiRouterOptions _options;

    public OpenApiRouterApiClient(HttpClient httpClient, IOptions<OpenApiRouterOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<JsonDocument> PostAsync(
        string relativeUrl,
        object body,
        CancellationToken cancellationToken)
    {
        string apiKey = GetApiKey();
        string json = JsonSerializer.Serialize(body, JsonOptions);

        using HttpRequestMessage request = new(HttpMethod.Post, relativeUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
        string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"OpenApi Router request failed with status {(int)response.StatusCode}: {responseBody}");
        }

        // Detect non-JSON responses (HTML error pages or redirects) to provide clearer errors.
        string? contentType = response.Content.Headers.ContentType?.MediaType;

        if (!string.IsNullOrWhiteSpace(contentType) &&
            !contentType.Contains("json", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"OpenApi Router returned non-JSON content (Content-Type: {contentType}). Response body: {responseBody}");
        }

        // Extra safety: some endpoints return HTML starting with '<'. Detect and fail.
        if (!string.IsNullOrWhiteSpace(responseBody) && responseBody.TrimStart().StartsWith('<'))
        {
            throw new InvalidOperationException(
                $"OpenApi Router returned an HTML response instead of JSON. Response body starts with '<'. Response body: {responseBody}");
        }

        return JsonDocument.Parse(responseBody);
    }

    private string GetApiKey()
    {
        string? apiKey = _options.ApiKey;

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            apiKey = Environment.GetEnvironmentVariable(_options.ApiKeyEnvironmentVariable);
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                $"OpenApi Router API key is missing. Set {OpenApiRouterOptions.SectionName}:ApiKey or the '{_options.ApiKeyEnvironmentVariable}' environment variable.");
        }

        return apiKey.Trim();
    }
}
