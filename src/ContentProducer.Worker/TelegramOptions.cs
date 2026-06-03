namespace ContentProducer.Worker;

public sealed class TelegramOptions
{
    public const string SectionName = "Telegram";

    public string BotApiBaseUrl { get; init; } = "https://api.telegram.org/";

    public string? BotToken { get; init; }

    public string BotTokenEnvironmentVariable { get; init; } = "TELEGRAM_BOT_TOKEN";

    public string ChannelChatId { get; init; } = string.Empty;
}
