using CreditBureau.Contracts.AsokiLoanApplications.CreditRegistration.CreditAgreementsAndLeasing.Requests;
using CreditBureau.Contracts.AsokiLoanApplications.CreditRegistration.CreditAgreementsAndLeasing.Responses;
using CreditBureau.Contracts.Common;
using Domain.Common.Constants;
using Domain.Common.Settings;
using Infrastructure.Common.Helpers.JsonHelpes;
using Infrastructure.Common.Helpers.Logger;
using Infrastructure.Services.Notifications;
using Microsoft.Extensions.Logging;
using BankHeader = CreditBureau.Contracts.AsokiLoanApplications.CreditRegistration.CreditApplications.BankHeader;
using RequestSecurity = CreditBureau.Contracts.Common.RequestSecurity;

namespace Infrastructure.Services.CreditBureauReportServices.Handlers;

/// <summary>
/// Обработчик CI-004: Сведения о кредитном договоре
/// </summary>
public class Ci004CreditRegistrationRequestHandler : CiHandlerBase<CreditRegistrationRequest>
{
    public Ci004CreditRegistrationRequestHandler(
        Application.Repositories.CreditBureauReportRepositories.ICreditBureauReportRepository creditBureauReportRepository,
        HttpClients.IRequestManagerService requestManagerService,
        Domain.Common.Settings.AsokiReportApiOptions asokiReportApiOptions,
        Domain.Common.Settings.AsokiApplicationApiOptions asokiApplicationApiOptions,
        RequestSecurity requestSecurity,
        BankHeader bankHeader,
        Common.Helpers.Logger.LogWriter logWriter,
        ITelegramNotificationService telegramNotificationService,
        ILogger<Ci004CreditRegistrationRequestHandler> logger)
        : base(creditBureauReportRepository, requestManagerService, asokiReportApiOptions, asokiApplicationApiOptions, requestSecurity, bankHeader, logWriter, telegramNotificationService, logger)
    {
    }

    public override int CiCode => 4;

    public override async Task<CiProcessingResult> ProcessAsync(CancellationToken cancellationToken)
    {
        var requests = await CreditBureauReportRepository.GetCreditRegistrationRequestsAsync(cancellationToken);
        Logger.LogInformation("CI-{CiCode} queue loaded. Count={Count}", CiCode, requests.Count);

        var result = new CiProcessingResult();

        foreach (var item in requests)
        {
            result.Processed++;

            if (item.Request is null)
            {
                result.Error++;
                Logger.LogWarning("CI-{CiCode} skipped due to null request. LoanKey={LoanKey}", CiCode, item.LoanKey);
                await CreditBureauReportRepository.UpsertCiStatusAsync(
                    item.LoanKey, CiCode, 2, "CI-004 request is null", null, cancellationToken);
                continue;
            }

            try
            {
                item.Request.PDate = FormatKatmIsoDateAtStartOfDay(DateTimeOffset.Now);
                SetStandardFields(item.Request, item.Request.PDate);

                var baseRequest = CreateBaseRequest(item.Request);

                var response = await RequestManagerService.SendPostRequest(
                    AsokiApplicationApiOptions.HostAddress + AsokiApplicationApiOptions.CreditRegistrationUrl,
                    baseRequest.ToJSON(),
                    item.LoanKey,
                    cancellationToken);

                if (string.IsNullOrWhiteSpace(response))
                {
                    result.Error++;
                    await CreditBureauReportRepository.UpsertCiStatusAsync(
                        item.LoanKey, CiCode, 2, "CI-004 returned empty response", null, cancellationToken);
                    continue;
                }

                if (!IsJsonResponse(response))
                {
                    result.Error++;
                    var invalidFormatMessage = $"Invalid response from API: {GetResponsePreview(response, 200)}";
                    await NotifyErrorAsync($"CI-{CiCode:D3} invalid response format", item.LoanKey, GetResponsePreview(response, 1500), cancellationToken);
                    await CreditBureauReportRepository.UpsertCiStatusAsync(
                        item.LoanKey, CiCode, 2, invalidFormatMessage, null, cancellationToken);
                    continue;
                }

                var wrappedResponse = TryDeserializeJson<BaseResponse<CreditRegistrationResponse>>(response);
                var baseResponse = wrappedResponse?.data ?? TryDeserializeJson<CreditRegistrationResponse>(response);
                if (baseResponse is null)
                {
                    result.Error++;
                    var invalidJsonMessage = $"Invalid JSON structure from API: {GetResponsePreview(response, 200)}";
                    await NotifyErrorAsync($"CI-{CiCode:D3} invalid JSON structure", item.LoanKey, GetResponsePreview(response, 1500), cancellationToken);
                    await CreditBureauReportRepository.UpsertCiStatusAsync(
                        item.LoanKey, CiCode, 2, invalidJsonMessage, null, cancellationToken);
                    continue;
                }
                var isSuccess = baseResponse?.result is CreditBureauResultCodes.SUCCESS_00000 or CreditBureauResultCodes.SUCCESS_05000;
                var message = baseResponse?.resultMessage ?? wrappedResponse?.errorMessage ?? (isSuccess ? "Success" : "Unknown error");
                var ciStatus = (byte)(isSuccess ? 1 : 2);

                if (isSuccess)
                {
                    result.Success++;
                    Logger.LogInformation("LoanKey:{LoanKey} CI-{CiCode} success. Message:{Message}", item.LoanKey, CiCode, message);
                    LogWriter.Log("CreditRegistrationAgreement.txt", $"LoanKey:{item.LoanKey} CI-{CiCode:D3} success. Message:{message}");
                }
                else
                {
                    result.Error++;
                    await NotifyErrorAsync($"CI-{CiCode:D3} API error", item.LoanKey, $"Message: {message}\nResponse: {GetResponsePreview(response, 1500)}", cancellationToken);
                    Logger.LogError("LoanKey:{LoanKey} CI-{CiCode} error. Response:{Response}", item.LoanKey, CiCode, response);
                    LogWriter.Log("CreditRegistrationAgreement.txt", $"LoanKey:{item.LoanKey} CI-{CiCode:D3} error. Response:{response}");
                }

                await CreditBureauReportRepository.UpsertCiStatusAsync(
                    item.LoanKey, CiCode, ciStatus, message, null, cancellationToken);
            }
            catch (Exception ex)
            {
                result.Error++;
                await NotifyErrorAsync($"CI-{CiCode:D3} processing exception", item.LoanKey, $"Message: {ex.Message}\nStackTrace: {ex.StackTrace}", cancellationToken);
                Logger.LogError(ex, "CI-{CiCode} error processing LoanKey={LoanKey}. Error={Error}", CiCode, item.LoanKey, ex.Message);
                await CreditBureauReportRepository.UpsertCiStatusAsync(
                    item.LoanKey, CiCode, 2, $"CI-{CiCode:D3} processing error: {ex.Message}", null, cancellationToken);
            }
        }

        Logger.LogInformation(
            "CI-{CiCode} completed. Processed={Processed}, Success={Success}, Error={Error}",
            CiCode, result.Processed, result.Success, result.Error);

        return result;
    }
}
