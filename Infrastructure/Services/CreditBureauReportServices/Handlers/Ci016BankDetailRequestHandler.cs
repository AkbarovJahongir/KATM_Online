using CreditBureauService.Contracts.CreditBureauApplications.CreditRegistration.CreditAgreementsAndLeasing.Requests;
using Infrastructure.Common.Helpers.Logger;
using Infrastructure.Services.HttpClients;
using Infrastructure.Services.Notifications;
using Microsoft.Extensions.Logging;
using BankHeader = CreditBureauService.Contracts.CreditBureauApplications.CreditRegistration.CreditApplications.BankHeader;
using RequestSecurity = CreditBureauService.Contracts.Common.RequestSecurity;

namespace Infrastructure.Services.CreditBureauReportServices.Handlers;

/// <summary>
/// Обработчик CI-016: банковские реквизиты погашения
/// </summary>
public class Ci016BankDetailRequestHandler : CiHandlerBase<CreditRegistrationBankDitailRequest>, ICiPeriodHandler
{
    public Ci016BankDetailRequestHandler(
        Application.Repositories.CreditBureauReportRepositories.ICreditBureauReportRepository creditBureauReportRepository,
        IRequestManagerService requestManagerService,
        Domain.Common.Settings.CreditBureauReportApiOptions creditBureauReportApiOptions,
        Domain.Common.Settings.CreditBureauApiOptions creditBureauApiOptions,
        RequestSecurity requestSecurity,
        BankHeader bankHeader,
        LogWriter logWriter,
        ITelegramNotificationService telegramNotificationService,
        ILogger<Ci016BankDetailRequestHandler> logger)
        : base(creditBureauReportRepository, requestManagerService, creditBureauReportApiOptions, creditBureauApiOptions,
            requestSecurity, bankHeader, logWriter, telegramNotificationService, logger)
    {
    }

    public override int CiCode => 16;

    public override Task<CiProcessingResult> ProcessAsync(CancellationToken cancellationToken)
    {
        return ProcessCiRequestsAsync(
            CreditBureauReportRepository.GetCreditRegistrationBankDetailsRequestsAsync,
            CreateBaseRequest,
            CreditBureauApiOptions.HostAddress + CreditBureauApiOptions.CreditRegistrationRepaymentBankDitailUrl,
            "CreditRegistrationAgreement.txt",
            request =>
            {
                request.PDate = FormatKatmIsoDateAtStartOfDay(DateTimeOffset.Now);
                SetStandardFields(request);
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
            ct => CreditBureauReportRepository.GetCreditRegistrationBankDetailsRequestsByPeriodAsync(
                startDate, endDate, loanKey, ct),
            CreateBaseRequest,
            CreditBureauApiOptions.HostAddress + CreditBureauApiOptions.CreditRegistrationRepaymentBankDitailUrl,
            "CreditRegistrationAgreement.txt",
            request => SetStandardFields(request),
            cancellationToken);
    }
}
