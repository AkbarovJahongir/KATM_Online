using CreditBureauService.Contracts.CreditBureauApplications.CreditRegistration.CreditAgreementsAndLeasing.Requests;
using Infrastructure.Common.Helpers.Logger;
using Infrastructure.Services.HttpClients;
using Infrastructure.Services.Notifications;
using Microsoft.Extensions.Logging;
using BankHeader = CreditBureauService.Contracts.CreditBureauApplications.CreditRegistration.CreditApplications.BankHeader;
using RequestSecurity = CreditBureauService.Contracts.Common.RequestSecurity;

namespace Infrastructure.Services.CreditBureauReportServices.Handlers;

/// <summary>
/// Обработчик CI-024: остатки и обороты по балансовым счетам (ТЗ 10.0)
/// </summary>
public class Ci024BalanceReportHandler : CiHandlerBase<CreditRegistrationBalanceReport>
{
    public Ci024BalanceReportHandler(
        Application.Repositories.CreditBureauReportRepositories.ICreditBureauReportRepository creditBureauReportRepository,
        IRequestManagerService requestManagerService,
        Domain.Common.Settings.CreditBureauReportApiOptions creditBureauReportApiOptions,
        Domain.Common.Settings.CreditBureauApiOptions creditBureauApiOptions,
        RequestSecurity requestSecurity,
        BankHeader bankHeader,
        LogWriter logWriter,
        ITelegramNotificationService telegramNotificationService,
        ILogger<Ci024BalanceReportHandler> logger)
        : base(creditBureauReportRepository, requestManagerService, creditBureauReportApiOptions, creditBureauApiOptions,
            requestSecurity, bankHeader, logWriter, telegramNotificationService, logger)
    {
    }

    public override int CiCode => 24;

    public override Task<CiProcessingResult> ProcessAsync(CancellationToken cancellationToken)
    {
        // URL метода в ТЗ 10.0 не указан — пока не задан в настройках, не отправляем.
        if (string.IsNullOrWhiteSpace(CreditBureauApiOptions.BalanceReportUrl))
        {
            Logger.LogInformation("CI-{CiCode} skipped: CreditBureauApiOptions.BalanceReportUrl is not configured", CiCode);
            return Task.FromResult(new CiProcessingResult());
        }

        return ProcessCiRequestsAsync(
            CreditBureauReportRepository.GetBalanceReportRequestsAsync,
            CreateBaseRequest,
            CreditBureauApiOptions.HostAddress + CreditBureauApiOptions.BalanceReportUrl,
            "CreditRegistrationBalanceReport.txt",
            request =>
            {
                // pDate — дата баланса из БД, не дата отправки: не перезаписываем.
                SetStandardFields(request, setPDate: false);
            },
            cancellationToken);
    }
}
