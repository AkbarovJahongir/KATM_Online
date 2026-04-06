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
/// Обработчик CI-016: Сведения о платежных документах
/// </summary>
public class Ci016BankDetailRequestHandler : CiHandlerBase<CreditRegistrationBankDitailRequest>
{
    public Ci016BankDetailRequestHandler(
        Application.Repositories.CreditBureauReportRepositories.ICreditBureauReportRepository creditBureauReportRepository,
        HttpClients.IRequestManagerService requestManagerService,
        Domain.Common.Settings.AsokiReportApiOptions asokiReportApiOptions,
        Domain.Common.Settings.AsokiApplicationApiOptions asokiApplicationApiOptions,
        RequestSecurity requestSecurity,
        BankHeader bankHeader,
        Common.Helpers.Logger.LogWriter logWriter,
        ILogger<Ci016BankDetailRequestHandler> logger)
        : base(creditBureauReportRepository, requestManagerService, asokiReportApiOptions, asokiApplicationApiOptions, requestSecurity, bankHeader, logWriter, logger)
    {
    }

    public override int CiCode => 16;

    public override async Task<CiProcessingResult> ProcessAsync(CancellationToken cancellationToken)
    {
        var requests = await CreditBureauReportRepository.GetCreditRegistrationBankDetailsRequestsAsync(cancellationToken);
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
                    item.LoanKey, CiCode, 2, "CI-016 request is null", null, cancellationToken);
                continue;
            }

            try
            {
                SetStandardFields(item.Request);
                
                var baseRequest = CreateBaseRequest(item.Request);

                var response = await RequestManagerService.SendPostRequest(
                    AsokiApplicationApiOptions.HostAddress + AsokiApplicationApiOptions.CreditRegistrationRepaymentBankDitailUrl,
                    baseRequest.ToJSON(),
                    item.LoanKey,
                    cancellationToken);

                if (string.IsNullOrWhiteSpace(response))
                {
                    result.Error++;
                    Logger.LogError("CI-{CiCode} empty response. LoanKey={LoanKey}", CiCode, item.LoanKey);
                    await CreditBureauReportRepository.UpsertCiStatusAsync(
                        item.LoanKey, CiCode, 2, "CI-016 returned empty response", null, cancellationToken);
                    continue;
                }

                var wrappedResponse = JsonConvert.DeserializeObject<BaseResponse<CreditRegistrationResponse>>(response);
                var baseResponse = wrappedResponse?.data ?? JsonConvert.DeserializeObject<CreditRegistrationResponse>(response);
                var isSuccess = baseResponse.result is CreditBureauResultCodes.SUCCESS_00000 or CreditBureauResultCodes.SUCCESS_05000;
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
                    Logger.LogError("LoanKey:{LoanKey} CI-{CiCode} error. Response:{Response}", item.LoanKey, CiCode, response);
                    LogWriter.Log("CreditRegistrationAgreement.txt", $"LoanKey:{item.LoanKey} CI-{CiCode:D3} error. Response:{response}");
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
    /// Обработка и отправка отчета CI-016 за указанный период
    /// </summary>
    /// <param name="startDate">Дата начала периода</param>
    /// <param name="endDate">Дата окончания периода</param>
    /// <param name="cancellationToken">Токен отмены</param>
    public async Task<CiProcessingResult> SendByPeriodAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
    {
        var requests = await CreditBureauReportRepository.GetCreditRegistrationBankDetailsRequestsByPeriodAsync(startDate, endDate, cancellationToken);
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
                    item.LoanKey, CiCode, 2, "CI-016 request is null", null, cancellationToken);
                continue;
            }

            try
            {
                SetStandardFields(item.Request);

                var baseRequest = CreateBaseRequest(item.Request);

                var response = await RequestManagerService.SendPostRequest(
                    AsokiApplicationApiOptions.HostAddress + AsokiApplicationApiOptions.CreditRegistrationRepaymentBankDitailUrl,
                    baseRequest.ToJSON(),
                    item.LoanKey,
                    cancellationToken);

                if (string.IsNullOrWhiteSpace(response))
                {
                    result.Error++;
                    Logger.LogError("CI-{CiCode} empty response. LoanKey={LoanKey}", CiCode, item.LoanKey);
                    await CreditBureauReportRepository.UpsertCiStatusAsync(
                        item.LoanKey, CiCode, 2, "CI-016 returned empty response", null, cancellationToken);
                    continue;
                }

                var wrappedResponse = JsonConvert.DeserializeObject<BaseResponse<CreditRegistrationResponse>>(response);
                var baseResponse = wrappedResponse?.data ?? JsonConvert.DeserializeObject<CreditRegistrationResponse>(response);
                var isSuccess = baseResponse.result is CreditBureauResultCodes.SUCCESS_00000 or CreditBureauResultCodes.SUCCESS_05000;
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
                    Logger.LogError("LoanKey:{LoanKey} CI-{CiCode} error. Response:{Response}", item.LoanKey, CiCode, response);
                    LogWriter.Log("CreditRegistrationAgreement.txt", $"LoanKey:{item.LoanKey} CI-{CiCode:D3} error. Response:{response}");
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
