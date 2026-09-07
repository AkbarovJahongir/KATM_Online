using Application.Repositories.CreditBureauReportRepositories;
using Domain.Common.Settings;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text;
using System.Text.RegularExpressions;

namespace Infrastructure.Services.Notifications;

public sealed class TelegramNotificationService(
    IHttpClientFactory httpClientFactory,
    ICreditBureauReportRepository creditBureauReportRepository,
    TelegramNotificationSettings settings,
    ILogger<TelegramNotificationService> logger) : ITelegramNotificationService
{
    private const int TelegramMessageLimit = 4000;
    private static readonly Regex LoanKeyRegex = new(
        @"(?:LoanKey|Key_RequestHistory|KeyLoanHistoryKb)\s*:\s*(\S+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly ICreditBureauReportRepository _creditBureauReportRepository = creditBureauReportRepository;
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

    private async Task SendAsync(
        string title,
        string source,
        string message,
        string? app,
        string? customerId,
        CancellationToken cancellationToken)
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

        (app, customerId) = await ResolveLoanContextAsync(app, customerId, message, cancellationToken);
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

    private async Task<(string? App, string? CustomerId)> ResolveLoanContextAsync(
        string? app,
        string? customerId,
        string message,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(app) && !string.IsNullOrWhiteSpace(customerId))
        {
            return (app, customerId);
        }

        var loanKey = TryExtractLoanKey(message);
        if (string.IsNullOrWhiteSpace(loanKey))
        {
            return (app, customerId);
        }

        try
        {
            var (resolvedApp, resolvedCustomerId) =
                await _creditBureauReportRepository.GetLoanAppAndCustomerIdAsync(loanKey, cancellationToken);

            return (
                string.IsNullOrWhiteSpace(app) ? resolvedApp : app,
                string.IsNullOrWhiteSpace(customerId) ? resolvedCustomerId : customerId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve App/Customer_ID for LoanKey={LoanKey}", loanKey);
            return (app, customerId);
        }
    }

    private static string? TryExtractLoanKey(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return null;
        }

        var match = LoanKeyRegex.Match(message);
        return match.Success ? match.Groups[1].Value.Trim() : null;
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
