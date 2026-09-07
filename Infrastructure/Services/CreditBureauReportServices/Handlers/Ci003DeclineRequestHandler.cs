using Application.Repositories.CreditBureauReportRepositories;
using CreditBureauService.Contracts.CreditBureauApplications.CreditRegistration.CreditAgreementsAndLeasing.Requests;
using Infrastructure.Common.Helpers.Logger;
using Infrastructure.Services.HttpClients;
using Infrastructure.Services.Notifications;
using Microsoft.Extensions.Logging;
using BankHeader = CreditBureauService.Contracts.CreditBureauApplications.CreditRegistration.CreditApplications.BankHeader;
using RequestSecurity = CreditBureauService.Contracts.Common.RequestSecurity;

namespace Infrastructure.Services.CreditBureauReportServices.Handlers;

/// <summary>
/// Обработчик CI-003: Отклонение заявки
/// </summary>
public class Ci003DeclineRequestHandler : CiHandlerBase<CreditRegistrationDeclineRequest>
{
    public Ci003DeclineRequestHandler(
        ICreditBureauReportRepository creditBureauReportRepository,
        IRequestManagerService requestManagerService,
        Domain.Common.Settings.CreditBureauReportApiOptions creditBureauReportApiOptions,
        Domain.Common.Settings.CreditBureauApiOptions creditBureauApiOptions,
        RequestSecurity requestSecurity,
        BankHeader bankHeader,
        LogWriter logWriter,
        ITelegramNotificationService telegramNotificationService,
        ILogger<Ci003DeclineRequestHandler> logger)
        : base(creditBureauReportRepository, requestManagerService, creditBureauReportApiOptions, creditBureauApiOptions,
            requestSecurity, bankHeader, logWriter, telegramNotificationService, logger)
    {
    }

    public override int CiCode => 3;

    public override Task<CiProcessingResult> ProcessAsync(CancellationToken cancellationToken)
    {
        return ProcessCiRequestsAsync(
            CreditBureauReportRepository.GetCreditRegistrationDeclineRequestsAsync,
            CreateBaseRequest,
            CreditBureauApiOptions.HostAddress + CreditBureauApiOptions.DeclineApplicationUrl,
            "CreditRegistrationDecline.txt",
            request => SetStandardFields(request),
            cancellationToken);
    }
}
