using Application.Repositories.CreditBureauReportRepositories;
using CreditBureau.Contracts.AsokiLoanApplications.CreditRegistration.CreditApplications;
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
/// Обработчик CI-002: Отправка заявки юр.лица в кредитное бюро
/// </summary>
public class Ci002EntityRequestHandler : CiHandlerBase<CreditRegistrationEntityRequest>
{
    public Ci002EntityRequestHandler(
        ICreditBureauReportRepository creditBureauReportRepository,
        IRequestManagerService requestManagerService,
        AsokiReportApiOptions asokiReportApiOptions,
        AsokiApplicationApiOptions asokiApplicationApiOptions,
        RequestSecurity requestSecurity,
        BankHeader bankHeader,
        LogWriter logWriter,
        ILogger<Ci002EntityRequestHandler> logger)
        : base(creditBureauReportRepository, requestManagerService, asokiReportApiOptions, asokiApplicationApiOptions, requestSecurity, bankHeader, logWriter, logger)
    {
    }

    public override int CiCode => 2;

    public override async Task<CiProcessingResult> ProcessAsync(CancellationToken cancellationToken)
    {
        var requests = await CreditBureauReportRepository.GetCreditRegistrationEntityRequestsAsync(cancellationToken);
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
                    item.LoanKey, CiCode, 2, "CI-002 request is null", null, cancellationToken);
                continue;
            }

            try
            {
                var baseRequest = new BaseRequestForCreditApplications<CreditRegistrationEntityRequest>
                {
                    Header = BankHeader,
                    Request = item.Request,
                    Security = RequestSecurity
                };

                var response = await RequestManagerService.SendPostRequest(
                    AsokiApplicationApiOptions.HostAddress + AsokiApplicationApiOptions.LegalEntityApplicationUrl,
                    baseRequest.ToJSON(),
                    item.LoanKey,
                    cancellationToken);

                if (string.IsNullOrWhiteSpace(response))
                {
                    result.Error++;
                    await CreditBureauReportRepository.UpsertCiStatusAsync(
                        item.LoanKey, CiCode, 2, "CI-002 returned empty response", null, cancellationToken);
                    continue;
                }

                var baseResponse = JsonConvert.DeserializeObject<CreditRegistrationSubjectResponse>(response);
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
                        "CreditRegistrationEntity.txt",
                        $"LoanKey:{item.LoanKey} CI-{CiCode:D3} success. KatmSir:\t{token}");
                }
                else
                {
                    result.Error++;
                    Logger.LogError("LoanKey:{LoanKey} CI-{CiCode} error. Response:{Response}", item.LoanKey, CiCode, response);
                    LogWriter.Log("CreditRegistrationEntity.txt", $"LoanKey:{item.LoanKey} CI-{CiCode:D3} error. Response:{response}");
                }

                await CreditBureauReportRepository.UpsertCiStatusAsync(
                    item.LoanKey, CiCode, ciStatus, message, token, cancellationToken);
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
}
