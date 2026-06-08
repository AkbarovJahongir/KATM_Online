using Domain.Common.Settings;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text;

namespace Infrastructure.Services.Notifications;

public sealed class TelegramNotificationService(
    IHttpClientFactory httpClientFactory,
    TelegramNotificationSettings settings,
    ILogger<TelegramNotificationService> logger) : ITelegramNotificationService
{
    private const int TelegramMessageLimit = 4000;

    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly TelegramNotificationSettings _settings = settings;
    private readonly ILogger<TelegramNotificationService> _logger = logger;

    public Task NotifyAsync(string title, string source, string message, CancellationToken cancellationToken = default)
    {
        return SendAsync(title, source, message, null, null, cancellationToken);
    }

    public Task NotifyAsync(string title, string source, string message, string? app, string? customerId, CancellationToken cancellationToken = default)
    {
        return SendAsync(title, source, message, app, customerId, cancellationToken);
    }

    public async Task NotifyErrorAsync(string source, string message, CancellationToken cancellationToken = default)
    {
        await SendAsync("KATM_Online error", source, message, null, null, cancellationToken);
    }

    public async Task NotifyErrorAsync(string source, string message, string? app, string? customerId, CancellationToken cancellationToken = default)
    {
        await SendAsync("KATM_Online error", source, message, app, customerId, cancellationToken);
    }

    private async Task SendAsync(string title, string source, string message, string? app, string? customerId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Telegram notification check: Enabled={Enabled}, BotToken present={BotTokenPresent}, ChatIds count={ChatIdsCount}",
            _settings.Enabled,
            !string.IsNullOrWhiteSpace(_settings.BotToken),
            (_settings.ChatIds?.Count ?? 0));

        if (!_settings.Enabled ||
            string.IsNullOrWhiteSpace(_settings.BotToken) ||
            _settings.ChatIds.Count == 0)
        {
            _logger.LogWarning("Telegram notifications disabled or not configured");
            return;
        }

        var text = BuildMessage(title, source, message, app, customerId);

        foreach (var chatId in _settings.ChatIds)
        {
            try
            {
                var payload = new
                {
                    chat_id = chatId,
                    text = text,
                    disable_web_page_preview = true
                };

                using var client = _httpClientFactory.CreateClient();
                using var response = await client.PostAsJsonAsync(
                    $"https://api.telegram.org/bot{_settings.BotToken}/sendMessage",
                    payload,
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning(
                        "Telegram notification failed. ChatId={ChatId}. StatusCode={StatusCode}. Response={Response}",
                        chatId,
                        (int)response.StatusCode,
                        responseText);
                }
                else
                {
                    _logger.LogInformation("Telegram notification sent successfully to ChatId={ChatId}", chatId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Telegram notification sending failed. ChatId={ChatId}", chatId);
            }
        }
    }

    private static string BuildMessage(string title, string source, string message, string? app = null, string? customerId = null)
    {
        var builder = new StringBuilder()
            .AppendLine(title)
            .Append("Source: ").AppendLine(source)
            .Append("Time: ").AppendLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

        builder.Append("APP: ").AppendLine(string.IsNullOrWhiteSpace(app) ? "N/A" : app);
        builder.Append("Customer_ID: ").AppendLine(string.IsNullOrWhiteSpace(customerId) ? "N/A" : customerId);

        builder
            .AppendLine()
            .AppendLine(message.Trim());

        var text = builder.ToString();
        return text.Length > TelegramMessageLimit
            ? text[..TelegramMessageLimit] + "..."
            : text;
    }
}
