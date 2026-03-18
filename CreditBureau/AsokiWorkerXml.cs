using Domain.Common.Settings;
using Infrastructure.Common.Helpers.Logger;
using Infrastructure.Services.AsokiXmlServices;

namespace CreditBureau
{
    public class AsokiXml(WorkerSettings workerSettings, LogWriter logWriter, IAsokiXmlService asokiXmlService) : BackgroundService
    {
        private readonly WorkerSettings _workerSettings = workerSettings;
        private readonly LogWriter _logWriter = logWriter;
        private readonly IAsokiXmlService _asokiXmlService = asokiXmlService;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logWriter.Log("WorkerServerStateXml.txt", "Start successful!");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _asokiXmlService.AsokiProcessingXml(stoppingToken);
                    await Task.Delay(_workerSettings.DelayMilliseconds, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logWriter.Log("WorkerServerStateXml.txt", "Catch AsokiProcessing message error: " + ex.Message + "\n" + ex.StackTrace);
                    await Task.Delay(_workerSettings.DelayMilliseconds, stoppingToken);
                }
            }
            _logWriter.Log("WorkerServerStateXml.txt", "Stop successful!");
        }
    }
}
