using Infrastructure.Services.Notifications;

namespace CreditBureauService;

public sealed class StartupTelegramNotificationHostedService(
    ITelegramNotificationService telegramNotificationService,
    IHostEnvironment hostEnvironment,
    ILogger<StartupTelegramNotificationHostedService> logger) : IHostedService
{
    private readonly ITelegramNotificationService _telegramNotificationService = telegramNotificationService;
    private readonly IHostEnvironment _hostEnvironment = hostEnvironment;
    private readonly ILogger<StartupTelegramNotificationHostedService> _logger = logger;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _telegramNotificationService.NotifyAsync(
                "KATM_Online started",
                "ApplicationStartup",
                $"Environment: {_hostEnvironment.EnvironmentName}\nMachine: {Environment.MachineName}\nStartedAt: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send startup telegram notification.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
