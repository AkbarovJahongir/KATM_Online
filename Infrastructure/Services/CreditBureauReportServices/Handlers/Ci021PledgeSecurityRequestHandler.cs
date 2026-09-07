using CreditBureauService.Contracts.CreditBureauApplications.CreditRegistration.CreditAgreementsAndLeasing.Requests;
using Infrastructure.Common.Helpers.Logger;
using Infrastructure.Services.HttpClients;
using Infrastructure.Services.Notifications;
using Microsoft.Extensions.Logging;
using BankHeader = CreditBureauService.Contracts.CreditBureauApplications.CreditRegistration.CreditApplications.BankHeader;
using RequestSecurity = CreditBureauService.Contracts.Common.RequestSecurity;

namespace Infrastructure.Services.CreditBureauReportServices.Handlers;

/// <summary>
/// Обработчик CI-021: Сведения об обеспечении залога
/// </summary>
public class Ci021PledgeSecurityRequestHandler : CiHandlerBase<CreditRegistrationPledgeSecurity>
{
    public Ci021PledgeSecurityRequestHandler(
        Application.Repositories.CreditBureauReportRepositories.ICreditBureauReportRepository creditBureauReportRepository,
        IRequestManagerService requestManagerService,
        Domain.Common.Settings.CreditBureauReportApiOptions creditBureauReportApiOptions,
        Domain.Common.Settings.CreditBureauApiOptions creditBureauApiOptions,
        RequestSecurity requestSecurity,
        BankHeader bankHeader,
        LogWriter logWriter,
        ITelegramNotificationService telegramNotificationService,
        ILogger<Ci021PledgeSecurityRequestHandler> logger)
        : base(creditBureauReportRepository, requestManagerService, creditBureauReportApiOptions, creditBureauApiOptions,
            requestSecurity, bankHeader, logWriter, telegramNotificationService, logger)
    {
    }

    public override int CiCode => 21;

    public override Task<CiProcessingResult> ProcessAsync(CancellationToken cancellationToken)
    {
        return ProcessCiRequestsAsync(
            CreditBureauReportRepository.GetPledgeSecurityRequestsAsync,
            CreateBaseRequest,
            CreditBureauApiOptions.HostAddress + CreditBureauApiOptions.CreditPledgeSecurityUrl,
            "CreditRegistrationPledgeSecurity.txt",
            request =>
            {
                request.PDate = FormatKatmIsoDateAtStartOfDay(DateTimeOffset.Now);
                SetStandardFields(request, request.PDate);
            },
            cancellationToken);
    }
}
