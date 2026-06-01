using Infrastructure.Services.Notifications;

namespace CreditBureauService;

public sealed class GlobalExceptionNotifier(
    ITelegramNotificationService telegramNotificationService,
    ILogger<GlobalExceptionNotifier> logger)
{
    private readonly ITelegramNotificationService _telegramNotificationService = telegramNotificationService;
    private readonly ILogger<GlobalExceptionNotifier> _logger = logger;
    private int _isRegistered;

    public void Register()
    {
        if (Interlocked.Exchange(ref _isRegistered, 1) == 1)
        {
            return;
        }

        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
    {
        var exception = args.ExceptionObject as Exception;
        var message = exception is null
            ? $"Unhandled exception object: {args.ExceptionObject}\nIsTerminating: {args.IsTerminating}"
            : $"Message: {exception.Message}\nIsTerminating: {args.IsTerminating}\nStackTrace: {exception}";

        NotifySafely("AppDomain.CurrentDomain.UnhandledException", message);
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs args)
    {
        NotifySafely(
            "TaskScheduler.UnobservedTaskException",
            $"Message: {args.Exception.Message}\nStackTrace: {args.Exception}");

        args.SetObserved();
    }

    private void NotifySafely(string source, string message)
    {
        try
        {
            _logger.LogError("Global exception captured. Source={Source}. Message={Message}", source, message);
            _telegramNotificationService.NotifyErrorAsync(source, message).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send global exception telegram notification. Source={Source}", source);
        }
    }
}
