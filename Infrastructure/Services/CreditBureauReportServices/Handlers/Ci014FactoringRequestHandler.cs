using CreditBureauService.Contracts.CreditBureauApplications.CreditRegistration.CreditAgreementsAndLeasing.Requests;
using CreditBureauService.Contracts.Common;
using Infrastructure.Common.Helpers.Logger;
using Infrastructure.Services.HttpClients;
using Infrastructure.Services.Notifications;
using Microsoft.Extensions.Logging;
using BankHeader = CreditBureauService.Contracts.CreditBureauApplications.CreditRegistration.CreditApplications.BankHeader;
using RequestSecurity = CreditBureauService.Contracts.Common.RequestSecurity;

namespace Infrastructure.Services.CreditBureauReportServices.Handlers;

/// <summary>
/// Обработчик CI-014: сведения о договорах факторинга.
/// </summary>
public class Ci014FactoringRequestHandler : CiHandlerBase<CreditRegistrationFactoring>
{
    public Ci014FactoringRequestHandler(
        Application.Repositories.CreditBureauReportRepositories.ICreditBureauReportRepository creditBureauReportRepository,
        IRequestManagerService requestManagerService,
        Domain.Common.Settings.CreditBureauReportApiOptions creditBureauReportApiOptions,
        Domain.Common.Settings.CreditBureauApiOptions creditBureauApiOptions,
        RequestSecurity requestSecurity,
        BankHeader bankHeader,
        LogWriter logWriter,
        ITelegramNotificationService telegramNotificationService,
        ILogger<Ci014FactoringRequestHandler> logger)
        : base(creditBureauReportRepository, requestManagerService, creditBureauReportApiOptions, creditBureauApiOptions,
            requestSecurity, bankHeader, logWriter, telegramNotificationService, logger)
    {
    }

    public override int CiCode => 14;

    public override Task<CiProcessingResult> ProcessAsync(CancellationToken cancellationToken)
    {
        return ProcessCiRequestsAsync(
            CreditBureauReportRepository.GetCreditRegistrationFactoringRequestsAsync,
            CreateBaseRequest,
            CreditBureauApiOptions.HostAddress + CreditBureauApiOptions.CreditFactroingUrl,
            "CreditRegistrationAgreement.txt",
            request =>
            {
                request.PDate = FormatKatmIsoDateAtStartOfDay(DateTimeOffset.Now);
                SetStandardFields(request);
            },
            cancellationToken);
    }
}
