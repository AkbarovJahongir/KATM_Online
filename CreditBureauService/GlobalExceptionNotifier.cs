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
        if (args.ExceptionObject is Exception exception)
        {
            NotifySafely(
                "AppDomain.CurrentDomain.UnhandledException",
                exception,
                context: $"Необработанное исключение. IsTerminating={args.IsTerminating}");
            return;
        }

        NotifySafely(
            "AppDomain.CurrentDomain.UnhandledException",
            TelegramErrorFormatter.Format(
                category: "Критическая ошибка процесса",
                reason: $"Необработанный объект исключения: {args.ExceptionObject}",
                action: "Проверьте логи приложения и перезапустите сервис при необходимости.",
                context: $"IsTerminating={args.IsTerminating}"));
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs args)
    {
        NotifySafely(
            "TaskScheduler.UnobservedTaskException",
            args.Exception,
            context: "Необработанное исключение в Task");

        args.SetObserved();
    }

    private void NotifySafely(string source, Exception exception, string? context)
    {
        try
        {
            _logger.LogError(exception, "Global exception captured. Source={Source}", source);
            _telegramNotificationService
                .NotifyErrorAsync(source, exception, context)
                .GetAwaiter()
                .GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send global exception telegram notification. Source={Source}", source);
        }
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
