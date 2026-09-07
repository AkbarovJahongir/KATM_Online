using Application.Repositories.CreditBureauReportRepositories;
using CreditBureauService.Contracts.CreditBureauApplications.CreditRegistration.CreditApplications;
using Domain.Common.Constants;
using Domain.Common.Settings;
using Infrastructure.Common.Helpers.Logger;
using Infrastructure.Services.HttpClients;
using Infrastructure.Services.Notifications;
using Microsoft.Extensions.Logging;
using BankHeader = CreditBureauService.Contracts.CreditBureauApplications.CreditRegistration.CreditApplications.BankHeader;
using RequestSecurity = CreditBureauService.Contracts.Common.RequestSecurity;

namespace Infrastructure.Services.CreditBureauReportServices.Handlers;

/// <summary>
/// Обработчик CI-001: Отправка заявки физ.лица в кредитное бюро
/// </summary>
public class Ci001IndividualRequestHandler : CiHandlerBase<CreditRegistrationIndividualRequest>
{
    public Ci001IndividualRequestHandler(
        ICreditBureauReportRepository creditBureauReportRepository,
        IRequestManagerService requestManagerService,
        CreditBureauReportApiOptions creditBureauReportApiOptions,
        CreditBureauApiOptions creditBureauApiOptions,
        RequestSecurity requestSecurity,
        BankHeader bankHeader,
        LogWriter logWriter,
        ITelegramNotificationService telegramNotificationService,
        ILogger<Ci001IndividualRequestHandler> logger)
        : base(creditBureauReportRepository, requestManagerService, creditBureauReportApiOptions, creditBureauApiOptions,
            requestSecurity, bankHeader, logWriter, telegramNotificationService, logger)
    {
    }

    public override int CiCode => 1;

    public override Task<CiProcessingResult> ProcessAsync(CancellationToken cancellationToken)
    {
        return ProcessCiRequestsAsync(
            CreditBureauReportRepository.GetCreditRegistrationIndividualRequestsAsync,
            CreateBaseRequestForCredit,
            CreditBureauApiOptions.HostAddress + CreditBureauApiOptions.IndividualPersonApplicationUrl,
            "CreditRegistrationIndividual.txt",
            request => SetStandardFields(request),
            cancellationToken);
    }

    protected override async Task<(bool IsSuccess, string Message, string? Token)> ProcessResponseAsync(
        string response,
        int loanKey,
        string logFileName,
        CancellationToken cancellationToken)
    {
        if (!IsJsonResponse(response))
        {
            Logger.LogWarning("LoanKey:{LoanKey} CI-{CiCode} invalid response format. Response:{Response}", loanKey,
                CiCode, GetResponsePreview(response, 500));
            await NotifyErrorAsync(
                $"CI-{CiCode:D3} invalid response format",
                loanKey,
                GetResponsePreview(response, 1500),
                cancellationToken);
            return (false, $"Invalid response from API: {GetResponsePreview(response, 200)}", null);
        }

        var baseResponse = TryDeserializeJson<CreditRegistrationSubjectResponse>(response);
        if (baseResponse is null)
        {
            await NotifyErrorAsync(
                $"CI-{CiCode:D3} invalid JSON structure",
                loanKey,
                GetResponsePreview(response, 1500),
                cancellationToken);
            return (false, $"Invalid JSON structure from API: {GetResponsePreview(response, 200)}", null);
        }

        var isSuccess = baseResponse.Result?.Code is CreditBureauResultCodes.SUCCESS_00000 or CreditBureauResultCodes.SUCCESS_05000;
        var token = baseResponse.Response?.KatmSir;
        var message = baseResponse.Result?.Message ?? (isSuccess ? "Success" : "Unknown error");

        if (isSuccess)
        {
            Logger.LogInformation(
                "LoanKey:{LoanKey} CI-{CiCode} success. KatmSir:{KatmSir}",
                loanKey, CiCode, token);
            LogWriter.Log(logFileName, $"LoanKey:{loanKey} CI-{CiCode:D3} success. KatmSir:\t{token}");
        }
        else
        {
            await NotifyErrorAsync(
                $"CI-{CiCode:D3} API error",
                loanKey,
                $"Message: {message}\nResponse: {GetResponsePreview(response, 1500)}",
                cancellationToken);
            Logger.LogError("LoanKey:{LoanKey} CI-{CiCode} error. Response:{Response}", loanKey, CiCode, response);
            LogWriter.Log(logFileName, $"LoanKey:{loanKey} CI-{CiCode:D3} error. Response:{response}");
        }

        return (isSuccess, message, token);
    }
}
