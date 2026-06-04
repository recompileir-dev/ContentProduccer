using Microsoft.Extensions.Options;

namespace ContentProducer.Worker;

public sealed class TelegramPublisherService : ITelegramPublisherService
{
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
        string? imageUrl,
        CancellationToken cancellationToken)
    {
        string botToken = GetBotToken(_options);

        if (string.IsNullOrWhiteSpace(_options.ChannelChatId))
        {
            throw new InvalidOperationException("Telegram:ChannelChatId is required.");
        }

        bool hasImage = !string.IsNullOrWhiteSpace(imageUrl);
        EnsureTelegramTextLength(caption, hasImage);

        using FormUrlEncodedContent content = new(
            hasImage
                ? new Dictionary<string, string>
                {
                    ["chat_id"] = _options.ChannelChatId,
                    ["photo"] = imageUrl!,
                    ["caption"] = caption,
                    ["parse_mode"] = "HTML"
                }
                : new Dictionary<string, string>
            {
                ["chat_id"] = _options.ChannelChatId,
                ["text"] = caption,
                ["parse_mode"] = "HTML"
            });
        string method = hasImage ? "sendPhoto" : "sendMessage";
        using HttpResponseMessage response = await _httpClient.PostAsync(
            BuildApiUri(botToken, method),
            content,
            cancellationToken);
        string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Telegram API request '{method}' failed with status " +
                $"{(int)response.StatusCode}: {responseBody}");
        }

        _logger.LogInformation(
            "Published Telegram {PostType} post to channel {ChannelChatId}.",
            hasImage ? "image" : "text",
            _options.ChannelChatId);
    }

    private static void EnsureTelegramTextLength(string caption, bool hasImage)
    {
        int maxLength = hasImage ? 1024 : 4096;

        if (caption.Length > maxLength)
        {
            throw new InvalidOperationException(
                $"Telegram text is too long. Max: {maxLength}, actual: {caption.Length}.");
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

        return botToken.Trim();
    }

    private Uri BuildApiUri(string botToken, string method)
    {
        string relativePath = $"./bot{botToken}/{method}";
        return new Uri(_httpClient.BaseAddress!, relativePath);
    }
}
