using Domain.Common.Settings;
using Infrastructure.Common.Helpers.Logger;
using Infrastructure.Services.AsokiServices;

namespace CreditBureau
{
    public class Asoki(WorkerSettings workerSettings, LogWriter logWriter, IAsokiService asokiService) : BackgroundService
    {
        private readonly WorkerSettings _workerSettings = workerSettings;
        private readonly LogWriter _logWriter = logWriter;
        private readonly IAsokiService _asokiService = asokiService;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logWriter.Log("WorkerServerState.txt", "Start successful!");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _asokiService.AsokiProcessing(stoppingToken);
                    await Task.Delay(_workerSettings.DelayMilliseconds, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logWriter.Log("WorkerServerState.txt", "Catch AsokiProcessing message error: " + ex.Message + "\n" + ex.StackTrace);
                    await Task.Delay(_workerSettings.DelayMilliseconds, stoppingToken);
                }
            }
            _logWriter.Log("WorkerServerState.txt", "Stop successful!");
        }
    }
}
