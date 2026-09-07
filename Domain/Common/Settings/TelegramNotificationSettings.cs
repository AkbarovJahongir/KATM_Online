namespace Domain.Common.Settings;

public sealed class TelegramNotificationSettings
{
    public bool Enabled { get; init; }
    public string BotToken { get; init; } = string.Empty;
    public IReadOnlyList<string> ChatIds { get; init; } = Array.Empty<string>();
}
