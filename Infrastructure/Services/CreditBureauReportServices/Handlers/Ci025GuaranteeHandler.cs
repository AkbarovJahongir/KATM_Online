using CreditBureauService.Contracts.CreditBureauApplications.CreditRegistration.CreditAgreementsAndLeasing.Requests;
using Infrastructure.Common.Helpers.Logger;
using Infrastructure.Services.HttpClients;
using Infrastructure.Services.Notifications;
using Microsoft.Extensions.Logging;
using BankHeader = CreditBureauService.Contracts.CreditBureauApplications.CreditRegistration.CreditApplications.BankHeader;
using RequestSecurity = CreditBureauService.Contracts.Common.RequestSecurity;

namespace Infrastructure.Services.CreditBureauReportServices.Handlers;

/// <summary>
/// Обработчик CI-025: сведения о гарантии (ТЗ 10.0)
/// </summary>
public class Ci025GuaranteeHandler : CiHandlerBase<CreditRegistrationGuarantee>
{
    public Ci025GuaranteeHandler(
        Application.Repositories.CreditBureauReportRepositories.ICreditBureauReportRepository creditBureauReportRepository,
        IRequestManagerService requestManagerService,
        Domain.Common.Settings.CreditBureauReportApiOptions creditBureauReportApiOptions,
        Domain.Common.Settings.CreditBureauApiOptions creditBureauApiOptions,
        RequestSecurity requestSecurity,
        BankHeader bankHeader,
        LogWriter logWriter,
        ITelegramNotificationService telegramNotificationService,
        ILogger<Ci025GuaranteeHandler> logger)
        : base(creditBureauReportRepository, requestManagerService, creditBureauReportApiOptions, creditBureauApiOptions,
            requestSecurity, bankHeader, logWriter, telegramNotificationService, logger)
    {
    }

    public override int CiCode => 25;

    public override Task<CiProcessingResult> ProcessAsync(CancellationToken cancellationToken)
    {
        // URL метода в ТЗ 10.0 не указан — пока не задан в настройках, не отправляем.
        if (string.IsNullOrWhiteSpace(CreditBureauApiOptions.GuaranteeUrl))
        {
            Logger.LogInformation("CI-{CiCode} skipped: CreditBureauApiOptions.GuaranteeUrl is not configured", CiCode);
            return Task.FromResult(new CiProcessingResult());
        }

        return ProcessCiRequestsAsync(
            CreditBureauReportRepository.GetGuaranteeRequestsAsync,
            CreateBaseRequest,
            CreditBureauApiOptions.HostAddress + CreditBureauApiOptions.GuaranteeUrl,
            "CreditRegistrationGuarantee.txt",
            request =>
            {
                request.PDate = FormatKatmIsoDateAtStartOfDay(DateTimeOffset.Now);
                SetStandardFields(request, request.PDate);
            },
            cancellationToken);
    }
}
