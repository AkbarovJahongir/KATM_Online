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
/// Обработчик CI-018: Сведения о статусе счетов
/// </summary>
public class Ci018AccountStatusRequestHandler : CiHandlerBase<CreditRegistrationAccountStatus>
{
    public Ci018AccountStatusRequestHandler(
        Application.Repositories.CreditBureauReportRepositories.ICreditBureauReportRepository creditBureauReportRepository,
        HttpClients.IRequestManagerService requestManagerService,
        Domain.Common.Settings.AsokiReportApiOptions asokiReportApiOptions,
        Domain.Common.Settings.AsokiApplicationApiOptions asokiApplicationApiOptions,
        RequestSecurity requestSecurity,
        BankHeader bankHeader,
        Common.Helpers.Logger.LogWriter logWriter,
        ITelegramNotificationService telegramNotificationService,
        ILogger<Ci018AccountStatusRequestHandler> logger)
        : base(creditBureauReportRepository, requestManagerService, asokiReportApiOptions, asokiApplicationApiOptions, requestSecurity, bankHeader, logWriter, telegramNotificationService, logger)
    {
    }

    public override int CiCode => 18;

    public override async Task<CiProcessingResult> ProcessAsync(CancellationToken cancellationToken)
    {
        var requests = await CreditBureauReportRepository.GetAccountStatusRequestsAsync(cancellationToken);
        Logger.LogInformation("CI-{CiCode} queue loaded. Count={Count}", CiCode, requests.Count);

        var result = new CiProcessingResult();

        foreach (var item in requests)
        {
            result.Processed++;

            if (item.Request is null)
            {
                result.Error++;
                result.AddDetail(item.LoanKey, false, "CI-018 request is null");
                Logger.LogWarning("CI-{CiCode} skipped due to null request. LoanKey={LoanKey}", CiCode, item.LoanKey);
                await CreditBureauReportRepository.UpsertCiStatusAsync(
                    item.LoanKey, CiCode, 2, "CI-018 request is null", null, cancellationToken);
                continue;
            }

            try
            {
                item.Request.PDate = FormatKatmIsoDateAtStartOfDay(DateTimeOffset.Now);
                SetStandardFields(item.Request, FormatKatmDate(System.DateTimeOffset.Now));

                var baseRequest = CreateBaseRequest(item.Request);

                var response = await RequestManagerService.SendPostRequest(
                    AsokiApplicationApiOptions.HostAddress + AsokiApplicationApiOptions.AccountStatusUrl,
                    baseRequest.ToJSON(),
                    item.LoanKey,
                    cancellationToken);

                if (string.IsNullOrWhiteSpace(response))
                {
                    result.Error++;
                    result.AddDetail(item.LoanKey, false, "CI-018 returned empty response");
                    Logger.LogError("CI-{CiCode} empty response. LoanKey={LoanKey}", CiCode, item.LoanKey);
                    await NotifyErrorAsync($"CI-{CiCode:D3} empty response", item.LoanKey, "API returned empty response", cancellationToken);
                    await CreditBureauReportRepository.UpsertCiStatusAsync(
                        item.LoanKey, CiCode, 2, "CI-018 returned empty response", null, cancellationToken);
                    continue;
                }

                if (!IsJsonResponse(response))
                {
                    result.Error++;
                    var invalidFormatMessage = $"Invalid response from API: {GetResponsePreview(response, 200)}";
                    result.AddDetail(item.LoanKey, false, invalidFormatMessage, response);
                    Logger.LogWarning("LoanKey:{LoanKey} CI-{CiCode} invalid response format. Response:{Response}", item.LoanKey, CiCode, GetResponsePreview(response, 500));
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
                    result.AddDetail(item.LoanKey, false, invalidJsonMessage, response);
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
                    result.AddDetail(item.LoanKey, true, message, response);
                    Logger.LogInformation("LoanKey:{LoanKey} CI-{CiCode} success.", item.LoanKey, CiCode);
                    LogWriter.Log("CreditRegistrationAccountStatus.txt", $"LoanKey:{item.LoanKey} CI-{CiCode:D3} success.");
                }
                else
                {
                    result.Error++;
                    result.AddDetail(item.LoanKey, false, message, response);
                    await NotifyErrorAsync($"CI-{CiCode:D3} API error", item.LoanKey, $"Message: {message}\nResponse: {GetResponsePreview(response, 1500)}", cancellationToken);
                    Logger.LogError("LoanKey:{LoanKey} CI-{CiCode} error. Response:{Response}", item.LoanKey, CiCode, response);
                    LogWriter.Log("CreditRegistrationAccountStatus.txt", $"LoanKey:{item.LoanKey} CI-{CiCode:D3} error. Response:{response}");
                }

                await CreditBureauReportRepository.UpsertCiStatusAsync(
                    item.LoanKey, CiCode, ciStatus, message, null, cancellationToken);
            }
            catch (Exception ex)
            {
                result.Error++;
                result.AddDetail(item.LoanKey, false, $"CI-{CiCode:D3} processing error: {ex.Message}");
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

    /// <summary>
    /// Обработка и отправка отчета CI-018 за указанный период
    /// </summary>
    /// <param name="startDate">Дата начала периода</param>
    /// <param name="endDate">Дата окончания периода</param>
    /// <param name="loanKey">Фильтр по LoanKey (необязательно)</param>
    /// <param name="cancellationToken">Токен отмены</param>
    public async Task<CiProcessingResult> SendByPeriodAsync(DateTime startDate, DateTime endDate, int? loanKey, CancellationToken cancellationToken)
    {
        var requests = await CreditBureauReportRepository.GetAccountStatusRequestsByPeriodAsync(startDate, endDate, loanKey, cancellationToken);
        Logger.LogInformation("CI-{CiCode} queue loaded for period {StartDate} - {EndDate}, LoanKey: {LoanKey}. Count={Count}", CiCode, startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"), loanKey, requests.Count);

        var result = new CiProcessingResult();

        foreach (var item in requests)
        {
            result.Processed++;

            if (item.Request is null)
            {
                result.Error++;
                result.AddDetail(item.LoanKey, false, "CI-018 request is null");
                Logger.LogWarning("CI-{CiCode} skipped due to null request. LoanKey={LoanKey}", CiCode, item.LoanKey);
                await CreditBureauReportRepository.UpsertCiStatusAsync(
                    item.LoanKey, CiCode, 2, "CI-018 request is null", null, cancellationToken);
                continue;
            }

            try
            {
                SetStandardFields(item.Request, FormatKatmDate(startDate));

                var baseRequest = CreateBaseRequest(item.Request);

                var response = await RequestManagerService.SendPostRequest(
                    AsokiApplicationApiOptions.HostAddress + AsokiApplicationApiOptions.AccountStatusUrl,
                    baseRequest.ToJSON(),
                    item.LoanKey,
                    cancellationToken);

                if (string.IsNullOrWhiteSpace(response))
                {
                    result.Error++;
                    result.AddDetail(item.LoanKey, false, "CI-018 returned empty response");
                    Logger.LogError("CI-{CiCode} empty response. LoanKey={LoanKey}", CiCode, item.LoanKey);
                    await NotifyErrorAsync($"CI-{CiCode:D3} empty response", item.LoanKey, "API returned empty response", cancellationToken);
                    await CreditBureauReportRepository.UpsertCiStatusAsync(
                        item.LoanKey, CiCode, 2, "CI-018 returned empty response", null, cancellationToken);
                    continue;
                }

                if (!IsJsonResponse(response))
                {
                    result.Error++;
                    var invalidFormatMessage = $"Invalid response from API: {GetResponsePreview(response, 200)}";
                    result.AddDetail(item.LoanKey, false, invalidFormatMessage, response);
                    Logger.LogWarning("LoanKey:{LoanKey} CI-{CiCode} invalid response format. Response:{Response}", item.LoanKey, CiCode, GetResponsePreview(response, 500));
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
                    result.AddDetail(item.LoanKey, false, invalidJsonMessage, response);
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
                    result.AddDetail(item.LoanKey, true, message, response);
                    Logger.LogInformation("LoanKey:{LoanKey} CI-{CiCode} success.", item.LoanKey, CiCode);
                    LogWriter.Log("CreditRegistrationAccountStatus.txt", $"LoanKey:{item.LoanKey} CI-{CiCode:D3} success.");
                }
                else
                {
                    result.Error++;
                    result.AddDetail(item.LoanKey, false, message, response);
                    await NotifyErrorAsync($"CI-{CiCode:D3} API error", item.LoanKey, $"Message: {message}\nResponse: {GetResponsePreview(response, 1500)}", cancellationToken);
                    Logger.LogError("LoanKey:{LoanKey} CI-{CiCode} error. Response:{Response}", item.LoanKey, CiCode, response);
                    LogWriter.Log("CreditRegistrationAccountStatus.txt", $"LoanKey:{item.LoanKey} CI-{CiCode:D3} error. Response:{response}");
                }

                await CreditBureauReportRepository.UpsertCiStatusAsync(
                    item.LoanKey, CiCode, ciStatus, message, null, cancellationToken);
            }
            catch (Exception ex)
            {
                result.Error++;
                result.AddDetail(item.LoanKey, false, $"CI-{CiCode:D3} processing error: {ex.Message}");
                await NotifyErrorAsync($"CI-{CiCode:D3} processing exception", item.LoanKey, $"Message: {ex.Message}\nStackTrace: {ex.StackTrace}", cancellationToken);
                Logger.LogError(ex, "CI-{CiCode} error processing LoanKey={LoanKey}. Error={Error}", CiCode, item.LoanKey, ex.Message);
                await CreditBureauReportRepository.UpsertCiStatusAsync(
                    item.LoanKey, CiCode, 2, $"CI-{CiCode:D3} processing error: {ex.Message}", null, cancellationToken);
            }
        }

        Logger.LogInformation(
            "CI-{CiCode} completed for period {StartDate} - {EndDate}, LoanKey: {LoanKey}. Processed={Processed}, Success={Success}, Error={Error}",
            CiCode, startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"), loanKey, result.Processed, result.Success, result.Error);

        return result;
    }
}
