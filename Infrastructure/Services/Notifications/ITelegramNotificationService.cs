namespace Infrastructure.Services.Notifications;

public interface ITelegramNotificationService
{
    Task NotifyAsync(string title, string source, string message, CancellationToken cancellationToken = default);

    Task NotifyAsync(string title, string source, string message, string? app, string? customerId, CancellationToken cancellationToken = default);

    Task NotifyErrorAsync(string source, string message, CancellationToken cancellationToken = default);

    Task NotifyErrorAsync(string source, string message, string? app, string? customerId, CancellationToken cancellationToken = default);
}
