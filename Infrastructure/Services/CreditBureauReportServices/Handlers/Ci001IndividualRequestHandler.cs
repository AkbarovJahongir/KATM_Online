using CreditBureauService.Contracts.CreditBureauApplications.CreditRegistration.CreditApplications;
using Domain.Common.Constants;
using Domain.Common.Settings;
using Infrastructure.Common.Helpers.JsonHelpes;
using Infrastructure.Common.Helpers.Logger;
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
        Application.Repositories.CreditBureauReportRepositories.ICreditBureauReportRepository creditBureauReportRepository,
        HttpClients.IRequestManagerService requestManagerService,
        Domain.Common.Settings.CreditBureauReportApiOptions CreditBureauReportApiOptions,
        Domain.Common.Settings.CreditBureauApiOptions CreditBureauApiOptions,
        RequestSecurity requestSecurity,
        BankHeader bankHeader,
        Common.Helpers.Logger.LogWriter logWriter,
        ITelegramNotificationService telegramNotificationService,
        ILogger<Ci001IndividualRequestHandler> logger)
        : base(creditBureauReportRepository, requestManagerService, CreditBureauReportApiOptions, CreditBureauApiOptions, requestSecurity, bankHeader, logWriter, telegramNotificationService, logger)
    {
    }

    public override int CiCode => 1;

    public override async Task<CiProcessingResult> ProcessAsync(CancellationToken cancellationToken)
    {
        var requests = await CreditBureauReportRepository.GetCreditRegistrationIndividualRequestsAsync(cancellationToken);
        Logger.LogInformation("CI-{CiCode} queue loaded. Count={Count}", CiCode, requests.Count);

        var result = new CiProcessingResult();

        foreach (var item in requests)
        {
            result.Processed++;

            if (item.Request is null)
            {
                result.Error++;
                Logger.LogWarning("CI-{CiCode} skipped due to null request. LoanKey={LoanKey}", CiCode, item.LoanKey);
                continue;
            }

            try
            {
                SetStandardFields(item.Request);

                var baseRequest = new BaseRequestForCreditApplications<CreditRegistrationIndividualRequest>
                {
                    Header = BankHeader,
                    Request = item.Request,
                    Security = RequestSecurity
                };
                _currentRequestJson = baseRequest.ToJSON();

                var response = await RequestManagerService.SendPostRequest(
                    CreditBureauApiOptions.HostAddress + CreditBureauApiOptions.IndividualPersonApplicationUrl,
                    _currentRequestJson,
                    item.LoanKey,
                    cancellationToken);

                if (string.IsNullOrWhiteSpace(response))
                {
                    result.Error++;
                    const string emptyResponseMessage = "CI-001 returned empty response";
                    await CreditBureauReportRepository.UpsertCiStatusAsync(
                        item.LoanKey, CiCode, 2, emptyResponseMessage, null, cancellationToken);
                    continue;
                }

                if (!IsJsonResponse(response))
                {
                    result.Error++;
                    var invalidFormatMessage = $"Invalid response from API: {GetResponsePreview(response, 200)}";
                    Logger.LogWarning("LoanKey:{LoanKey} CI-{CiCode} invalid response format. Response:{Response}", item.LoanKey, CiCode, GetResponsePreview(response, 500));
                    await NotifyErrorAsync($"CI-{CiCode:D3} invalid response format", item.LoanKey, GetResponsePreview(response, 1500), cancellationToken);
                    await CreditBureauReportRepository.UpsertCiStatusAsync(
                        item.LoanKey, CiCode, 2, invalidFormatMessage, null, cancellationToken);
                    continue;
                }

                var baseResponse = TryDeserializeJson<CreditRegistrationSubjectResponse>(response);
                if (baseResponse is null)
                {
                    result.Error++;
                    var invalidJsonMessage = $"Invalid JSON structure from API: {GetResponsePreview(response, 200)}";
                    await NotifyErrorAsync($"CI-{CiCode:D3} invalid JSON structure", item.LoanKey, GetResponsePreview(response, 1500), cancellationToken);
                    await CreditBureauReportRepository.UpsertCiStatusAsync(
                        item.LoanKey, CiCode, 2, invalidJsonMessage, null, cancellationToken);
                    continue;
                }

                var isSuccess = baseResponse?.Result?.Code is CreditBureauResultCodes.SUCCESS_00000 or CreditBureauResultCodes.SUCCESS_05000;
                var ciStatus = (byte)(isSuccess ? 1 : 2);
                var token = baseResponse?.Response?.KatmSir;
                var message = baseResponse?.Result?.Message ?? (isSuccess ? "Success" : "Unknown error");

                if (isSuccess)
                {
                    result.Success++;
                    Logger.LogInformation(
                        "LoanKey:{LoanKey} CI-{CiCode} success. KatmSir:{KatmSir}",
                        item.LoanKey, CiCode, token);
                    LogWriter.Log(
                        "CreditRegistrationIndividual.txt",
                        $"LoanKey:{item.LoanKey} CI-{CiCode:D3} success. KatmSir:\t{token}");
                }
                else
                {
                    result.Error++;
                    await NotifyErrorAsync($"CI-{CiCode:D3} API error", item.LoanKey, $"Message: {message}\nResponse: {GetResponsePreview(response, 1500)}", cancellationToken);
                    Logger.LogError(
                        "LoanKey:{LoanKey} CI-{CiCode} error. Response:{Response}",
                        item.LoanKey, CiCode, response);
                    LogWriter.Log(
                        "CreditRegistrationIndividual.txt",
                        $"LoanKey:{item.LoanKey} CI-{CiCode:D3} error. Response:{response}");
                }

                await CreditBureauReportRepository.UpsertCiStatusAsync(
                    item.LoanKey, CiCode, ciStatus, message, token, cancellationToken);
            }
            catch (Exception ex)
            {
                result.Error++;
                Logger.LogError(ex, "CI-{CiCode} error processing LoanKey={LoanKey}. Error={Error}", CiCode, item.LoanKey, ex.Message);
                await NotifyErrorAsync($"CI-{CiCode:D3} processing exception", item.LoanKey, $"Message: {ex.Message}\nStackTrace: {ex.StackTrace}", cancellationToken);
                await CreditBureauReportRepository.UpsertCiStatusAsync(
                    item.LoanKey, CiCode, 2, $"CI-{CiCode:D3} processing error: {ex.Message}", null, cancellationToken);
            }
            finally
            {
                _currentRequestJson = null;
            }
        }

        Logger.LogInformation(
            "CI-{CiCode} completed. Processed={Processed}, Success={Success}, Error={Error}",
            CiCode, result.Processed, result.Success, result.Error);

        return result;
    }
}




