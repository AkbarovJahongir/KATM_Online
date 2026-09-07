using CreditBureauService.Contracts.CreditBureauApplications.CreditRegistration.CreditAgreementsAndLeasing.Requests;
using Infrastructure.Common.Helpers.Logger;
using Infrastructure.Services.HttpClients;
using Infrastructure.Services.Notifications;
using Microsoft.Extensions.Logging;
using BankHeader = CreditBureauService.Contracts.CreditBureauApplications.CreditRegistration.CreditApplications.BankHeader;
using RequestSecurity = CreditBureauService.Contracts.Common.RequestSecurity;

namespace Infrastructure.Services.CreditBureauReportServices.Handlers;

/// <summary>
/// Обработчик CI-018: статусы счетов
/// </summary>
public class Ci018AccountStatusRequestHandler : CiHandlerBase<CreditRegistrationAccountStatus>, ICiPeriodHandler
{
    public Ci018AccountStatusRequestHandler(
        Application.Repositories.CreditBureauReportRepositories.ICreditBureauReportRepository creditBureauReportRepository,
        IRequestManagerService requestManagerService,
        Domain.Common.Settings.CreditBureauReportApiOptions creditBureauReportApiOptions,
        Domain.Common.Settings.CreditBureauApiOptions creditBureauApiOptions,
        RequestSecurity requestSecurity,
        BankHeader bankHeader,
        LogWriter logWriter,
        ITelegramNotificationService telegramNotificationService,
        ILogger<Ci018AccountStatusRequestHandler> logger)
        : base(creditBureauReportRepository, requestManagerService, creditBureauReportApiOptions, creditBureauApiOptions,
            requestSecurity, bankHeader, logWriter, telegramNotificationService, logger)
    {
    }

    public override int CiCode => 18;

    public override Task<CiProcessingResult> ProcessAsync(CancellationToken cancellationToken)
    {
        return ProcessCiRequestsAsync(
            CreditBureauReportRepository.GetAccountStatusRequestsAsync,
            CreateBaseRequest,
            CreditBureauApiOptions.HostAddress + CreditBureauApiOptions.AccountStatusUrl,
            "CreditRegistrationAccountStatus.txt",
            request =>
            {
                request.PDate = FormatKatmIsoDateAtStartOfDay(DateTimeOffset.Now);
                SetStandardFields(request, FormatKatmDate(DateTimeOffset.Now));
            },
            cancellationToken);
    }

    public Task<CiProcessingResult> SendByPeriodAsync(
        DateTime startDate,
        DateTime endDate,
        int? loanKey,
        CancellationToken cancellationToken)
    {
        return ProcessCiRequestsAsync(
            ct => CreditBureauReportRepository.GetAccountStatusRequestsByPeriodAsync(startDate, endDate, loanKey, ct),
            CreateBaseRequest,
            CreditBureauApiOptions.HostAddress + CreditBureauApiOptions.AccountStatusUrl,
            "CreditRegistrationAccountStatus.txt",
            request => SetStandardFields(request, FormatKatmDate(startDate)),
            cancellationToken);
    }
}
