using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace ContentProducer.Worker;

public sealed class OpenAiApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly OpenAiOptions _options;

    public OpenAiApiClient(HttpClient httpClient, IOptions<OpenAiOptions> options)
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
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
        string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            string errorSummary = BuildErrorSummary(responseBody);

            throw new InvalidOperationException(
                $"OpenAI API request failed with status {(int)response.StatusCode}: " +
                errorSummary);
        }

        return JsonDocument.Parse(responseBody);
    }

    private static string BuildErrorSummary(string responseBody)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(responseBody);

            if (!document.RootElement.TryGetProperty("error", out JsonElement error))
            {
                return responseBody;
            }

            string? code = error.TryGetProperty("code", out JsonElement codeElement)
                ? codeElement.GetString()
                : null;
            string? message = error.TryGetProperty("message", out JsonElement messageElement)
                ? messageElement.GetString()
                : null;

            if (string.Equals(code, "billing_hard_limit_reached", StringComparison.Ordinal))
            {
                return "OpenAI API billing hard limit has been reached. Add API credits or " +
                    "increase the project or organization usage limit.";
            }

            return string.IsNullOrWhiteSpace(code)
                ? message ?? responseBody
                : $"{code}: {message}";
        }
        catch (JsonException)
        {
            return responseBody;
        }
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
                $"OpenAI API key is missing. Set OpenAI:ApiKey or the " +
                $"'{_options.ApiKeyEnvironmentVariable}' environment variable.");
        }

        return apiKey;
    }
}
