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
        string imageUrl,
        CancellationToken cancellationToken)
    {
        string botToken = GetBotToken(_options);

        if (string.IsNullOrWhiteSpace(_options.ChannelChatId))
        {
            throw new InvalidOperationException("Telegram:ChannelChatId is required.");
        }

        EnsureTelegramCaptionLength(caption);

        using FormUrlEncodedContent content = new(
            new Dictionary<string, string>
            {
                ["chat_id"] = _options.ChannelChatId,
                ["photo"] = imageUrl,
                ["caption"] = caption,
                ["parse_mode"] = "HTML"
            });
        using HttpResponseMessage response = await _httpClient.PostAsync(
            $"bot{botToken}/sendPhoto",
            content,
            cancellationToken);
        string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Telegram API request 'sendPhoto' failed with status " +
                $"{(int)response.StatusCode}: {responseBody}");
        }

        _logger.LogInformation(
            "Published Telegram image post to channel {ChannelChatId}.",
            _options.ChannelChatId);
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
