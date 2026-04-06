using Application.Repositories.CreditBureauReportRepositories;
using CreditBureau.Contracts.AsokiLoanApplications.CreditRegistration.CreditAgreementsAndLeasing.Requests;
using CreditBureau.Contracts.AsokiLoanApplications.CreditRegistration.CreditAgreementsAndLeasing.Responses;
using CreditBureau.Contracts.Common;
using Domain.Common.Constants;
using Domain.Common.Settings;
using Infrastructure.Common.Helpers.JsonHelpes;
using Infrastructure.Common.Helpers.Logger;
using Infrastructure.Services.CreditBureauReportServices.Handlers;
using Infrastructure.Services.HttpClients;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RequestSecurity = CreditBureau.Contracts.Common.RequestSecurity;
using BankHeader = CreditBureau.Contracts.AsokiLoanApplications.CreditRegistration.CreditApplications.BankHeader;

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
        ILogger<Ci018AccountStatusRequestHandler> logger)
        : base(creditBureauReportRepository, requestManagerService, asokiReportApiOptions, asokiApplicationApiOptions, requestSecurity, bankHeader, logWriter, logger)
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
                Logger.LogWarning("CI-{CiCode} skipped due to null request. LoanKey={LoanKey}", CiCode, item.LoanKey);
                await CreditBureauReportRepository.UpsertCiStatusAsync(
                    item.LoanKey, CiCode, 2, "CI-018 request is null", null, cancellationToken);
                continue;
            }

            try
            {
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
                    Logger.LogError("CI-{CiCode} empty response. LoanKey={LoanKey}", CiCode, item.LoanKey);
                    await CreditBureauReportRepository.UpsertCiStatusAsync(
                        item.LoanKey, CiCode, 2, "CI-018 returned empty response", null, cancellationToken);
                    continue;
                }

                var wrappedResponse = JsonConvert.DeserializeObject<BaseResponse<CreditRegistrationResponse>>(response);
                var baseResponse = wrappedResponse?.data ?? JsonConvert.DeserializeObject<CreditRegistrationResponse>(response);
                var isSuccess = baseResponse?.result is CreditBureauResultCodes.SUCCESS_00000 or CreditBureauResultCodes.SUCCESS_05000;
                var message = baseResponse?.resultMessage ?? wrappedResponse?.errorMessage ?? (isSuccess ? "Success" : "Unknown error");
                var ciStatus = (byte)(isSuccess ? 1 : 2);

                if (isSuccess)
                {
                    result.Success++;
                    Logger.LogInformation("LoanKey:{LoanKey} CI-{CiCode} success.", item.LoanKey, CiCode);
                    LogWriter.Log("CreditRegistrationAccountStatus.txt", $"LoanKey:{item.LoanKey} CI-{CiCode:D3} success.");
                }
                else
                {
                    result.Error++;
                    Logger.LogError("LoanKey:{LoanKey} CI-{CiCode} error. Response:{Response}", item.LoanKey, CiCode, response);
                    LogWriter.Log("CreditRegistrationAccountStatus.txt", $"LoanKey:{item.LoanKey} CI-{CiCode:D3} error. Response:{response}");
                }

                await CreditBureauReportRepository.UpsertCiStatusAsync(
                    item.LoanKey, CiCode, ciStatus, message, null, cancellationToken);
            }
            catch (Exception ex)
            {
                result.Error++;
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
    /// <param name="cancellationToken">Токен отмены</param>
    public async Task<CiProcessingResult> SendByPeriodAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
    {
        var requests = await CreditBureauReportRepository.GetAccountStatusRequestsByPeriodAsync(startDate, endDate, cancellationToken);
        Logger.LogInformation("CI-{CiCode} queue loaded for period {StartDate} - {EndDate}. Count={Count}", CiCode, startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"), requests.Count);

        var result = new CiProcessingResult();

        foreach (var item in requests)
        {
            result.Processed++;

            if (item.Request is null)
            {
                result.Error++;
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
                    Logger.LogError("CI-{CiCode} empty response. LoanKey={LoanKey}", CiCode, item.LoanKey);
                    await CreditBureauReportRepository.UpsertCiStatusAsync(
                        item.LoanKey, CiCode, 2, "CI-018 returned empty response", null, cancellationToken);
                    continue;
                }

                var wrappedResponse = JsonConvert.DeserializeObject<BaseResponse<CreditRegistrationResponse>>(response);
                var baseResponse = wrappedResponse?.data ?? JsonConvert.DeserializeObject<CreditRegistrationResponse>(response);
                var isSuccess = baseResponse?.result is CreditBureauResultCodes.SUCCESS_00000 or CreditBureauResultCodes.SUCCESS_05000;
                var message = baseResponse?.resultMessage ?? wrappedResponse?.errorMessage ?? (isSuccess ? "Success" : "Unknown error");
                var ciStatus = (byte)(isSuccess ? 1 : 2);

                if (isSuccess)
                {
                    result.Success++;
                    Logger.LogInformation("LoanKey:{LoanKey} CI-{CiCode} success.", item.LoanKey, CiCode);
                    LogWriter.Log("CreditRegistrationAccountStatus.txt", $"LoanKey:{item.LoanKey} CI-{CiCode:D3} success.");
                }
                else
                {
                    result.Error++;
                    Logger.LogError("LoanKey:{LoanKey} CI-{CiCode} error. Response:{Response}", item.LoanKey, CiCode, response);
                    LogWriter.Log("CreditRegistrationAccountStatus.txt", $"LoanKey:{item.LoanKey} CI-{CiCode:D3} error. Response:{response}");
                }

                await CreditBureauReportRepository.UpsertCiStatusAsync(
                    item.LoanKey, CiCode, ciStatus, message, null, cancellationToken);
            }
            catch (Exception ex)
            {
                result.Error++;
                Logger.LogError(ex, "CI-{CiCode} error processing LoanKey={LoanKey}. Error={Error}", CiCode, item.LoanKey, ex.Message);
                await CreditBureauReportRepository.UpsertCiStatusAsync(
                    item.LoanKey, CiCode, 2, $"CI-{CiCode:D3} processing error: {ex.Message}", null, cancellationToken);
            }
        }

        Logger.LogInformation(
            "CI-{CiCode} completed for period {StartDate} - {EndDate}. Processed={Processed}, Success={Success}, Error={Error}",
            CiCode, startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"), result.Processed, result.Success, result.Error);

        return result;
    }
}
