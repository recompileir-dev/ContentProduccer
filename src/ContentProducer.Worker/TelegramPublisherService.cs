using System.Text.Json;
using Microsoft.Extensions.Options;

namespace ContentProducer.Worker;

public sealed class TelegramPublisherService : ITelegramPublisherService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly ILogger<TelegramPublisherService> _logger;
    private readonly TelegramOptions _options;

    public TelegramPublisherService(
        IOptions<TelegramOptions> options,
        ILogger<TelegramPublisherService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_options.BotApiBaseUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromMinutes(5)
        };
    }

    public async Task PublishPostAsync(
        string caption,
        IReadOnlyList<string> imageUrls,
        CancellationToken cancellationToken)
    {
        string botToken = GetBotToken(_options);

        if (string.IsNullOrWhiteSpace(_options.ChannelChatId))
        {
            throw new InvalidOperationException("Telegram:ChannelChatId is required.");
        }

        if (imageUrls.Count == 0)
        {
            throw new InvalidOperationException("Telegram publishing requires at least one image.");
        }

        EnsureTelegramCaptionLength(caption);

        if (imageUrls.Count == 1)
        {
            await SendPhotoAsync(botToken, caption, imageUrls[0], cancellationToken);
        }
        else
        {
            await SendMediaGroupAsync(botToken, caption, imageUrls, cancellationToken);
        }

        _logger.LogInformation(
            "Published Telegram post to channel {ChannelChatId} with {ImageCount} image(s).",
            _options.ChannelChatId,
            imageUrls.Count);
    }

    private async Task SendPhotoAsync(
        string botToken,
        string caption,
        string imageUrl,
        CancellationToken cancellationToken)
    {
        await PostFormAsync(
            botToken,
            "sendPhoto",
            new Dictionary<string, string>
            {
                ["chat_id"] = _options.ChannelChatId,
                ["photo"] = imageUrl,
                ["caption"] = caption,
                ["parse_mode"] = "HTML"
            },
            cancellationToken);
    }

    private async Task SendMediaGroupAsync(
        string botToken,
        string caption,
        IReadOnlyList<string> imageUrls,
        CancellationToken cancellationToken)
    {
        if (imageUrls.Count > 10)
        {
            throw new InvalidOperationException(
                "Telegram media group publishing supports up to 10 images.");
        }

        object[] media = imageUrls
            .Select((url, index) => index == 0
                ? (object)new
                {
                    type = "photo",
                    media = url,
                    caption,
                    parse_mode = "HTML"
                }
                : (object)new
                {
                    type = "photo",
                    media = url
                })
            .ToArray<object>();

        await PostFormAsync(
            botToken,
            "sendMediaGroup",
            new Dictionary<string, string>
            {
                ["chat_id"] = _options.ChannelChatId,
                ["media"] = JsonSerializer.Serialize(media, JsonOptions)
            },
            cancellationToken);
    }

    private async Task PostFormAsync(
        string botToken,
        string method,
        Dictionary<string, string> values,
        CancellationToken cancellationToken)
    {
        using FormUrlEncodedContent content = new(values);
        using HttpResponseMessage response = await _httpClient.PostAsync(
            $"bot{botToken}/{method}",
            content,
            cancellationToken);
        string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Telegram API request '{method}' failed with status " +
                $"{(int)response.StatusCode}: {responseBody}");
        }
    }

    private static void EnsureTelegramCaptionLength(string caption)
    {
        const int maxCaptionLength = 1024;

        if (caption.Length > maxCaptionLength)
        {
            throw new InvalidOperationException(
                $"Telegram caption is too long. Max: {maxCaptionLength}, actual: {caption.Length}.");
        }
    }

    private static string GetBotToken(TelegramOptions options)
    {
        string? botToken = options.BotToken;

        if (string.IsNullOrWhiteSpace(botToken))
        {
            botToken = Environment.GetEnvironmentVariable(
                options.BotTokenEnvironmentVariable);
        }

        if (string.IsNullOrWhiteSpace(botToken))
        {
            throw new InvalidOperationException(
                "Telegram bot token is missing. Set Telegram:BotToken or " +
                $"'{options.BotTokenEnvironmentVariable}'.");
        }

        return botToken;
    }
}
