using Application.Repositories.CreditBureauReportRepositories;
using CreditBureau.Contracts.AsokiLoanApplications.CreditRegistration.CreditAgreementsAndLeasing.Responses;
using CreditBureau.Contracts.AsokiLoanApplications.CreditRegistration.CreditApplications;
using CreditBureau.Contracts.Common;
using Domain.Common.Constants;
using Domain.Common.Settings;
using Infrastructure.Common.Helpers.JsonHelpes;
using Infrastructure.Common.Helpers.Logger;
using Infrastructure.Services.HttpClients;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RequestSecurity = CreditBureau.Contracts.Common.RequestSecurity;
using BankHeader = CreditBureau.Contracts.AsokiLoanApplications.CreditRegistration.CreditApplications.BankHeader;

namespace Infrastructure.Services.CreditBureauReportServices.Handlers;

/// <summary>
/// Базовый класс для обработчиков CI-запросов
/// </summary>
public abstract class CiHandlerBase<TRequest> : ICiHandler
{
    protected readonly ICreditBureauReportRepository CreditBureauReportRepository;
    protected readonly IRequestManagerService RequestManagerService;
    protected readonly AsokiReportApiOptions AsokiReportApiOptions;
    protected readonly AsokiApplicationApiOptions AsokiApplicationApiOptions;
    protected readonly RequestSecurity RequestSecurity;
    protected readonly BankHeader BankHeader;
    protected readonly LogWriter LogWriter;
    protected readonly ILogger<CiHandlerBase<TRequest>> Logger;

    protected CiHandlerBase(
        ICreditBureauReportRepository creditBureauReportRepository,
        IRequestManagerService requestManagerService,
        AsokiReportApiOptions asokiReportApiOptions,
        AsokiApplicationApiOptions asokiApplicationApiOptions,
        RequestSecurity requestSecurity,
        BankHeader bankHeader,
        LogWriter logWriter,
        ILogger<CiHandlerBase<TRequest>> logger)
    {
        CreditBureauReportRepository = creditBureauReportRepository;
        RequestManagerService = requestManagerService;
        AsokiReportApiOptions = asokiReportApiOptions;
        AsokiApplicationApiOptions = asokiApplicationApiOptions;
        RequestSecurity = requestSecurity;
        BankHeader = bankHeader;
        LogWriter = logWriter;
        Logger = logger;
    }

    public abstract int CiCode { get; }

    public abstract Task<CiProcessingResult> ProcessAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Универсальный метод обработки CI-запроса
    /// </summary>
    protected async Task<CiProcessingResult> ProcessCiRequestsAsync(
        Func<CancellationToken, Task<List<CreditBureauReportQueueItem<TRequest>>>> getRequestsFunc,
        Func<TRequest, BaseRequest<TRequest>> prepareRequestFunc,
        string endpoint,
        string logFileName,
        Action<TRequest>? beforeRequestAction = null,
        CancellationToken cancellationToken = default)
    {
        var requests = await getRequestsFunc(cancellationToken);
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
                    item.LoanKey, CiCode, 2, $"CI-{CiCode:D3} request is null", null, cancellationToken);
                continue;
            }

            try
            {
                beforeRequestAction?.Invoke(item.Request);

                var baseRequest = prepareRequestFunc(item.Request);
                var response = await RequestManagerService.SendPostRequest(
                    endpoint,
                    baseRequest.ToJSON(),
                    item.LoanKey,
                    cancellationToken);

                if (string.IsNullOrWhiteSpace(response))
                {
                    result.Error++;
                    Logger.LogError("CI-{CiCode} empty response. LoanKey={LoanKey}", CiCode, item.LoanKey);
                    await CreditBureauReportRepository.UpsertCiStatusAsync(
                        item.LoanKey, CiCode, 2, $"CI-{CiCode:D3} returned empty response", null, cancellationToken);
                    continue;
                }

                var (isSuccess, message, token) = await ProcessResponseAsync(response, item.LoanKey, logFileName, cancellationToken);
                var ciStatus = (byte)(isSuccess ? 1 : 2);

                if (isSuccess)
                {
                    result.Success++;
                }
                else
                {
                    result.Error++;
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

    /// <summary>
    /// Обработка ответа от кредитного бюро
    /// </summary>
    protected virtual async Task<(bool IsSuccess, string Message, string? Token)> ProcessResponseAsync(
        string response,
        int loanKey,
        string logFileName,
        CancellationToken cancellationToken)
    {
        var wrappedResponse = JsonConvert.DeserializeObject<BaseResponse<CreditRegistrationResponse>>(response);
        var baseResponse = wrappedResponse?.data ?? JsonConvert.DeserializeObject<CreditRegistrationResponse>(response);
        var isSuccess = baseResponse?.result is CreditBureauResultCodes.SUCCESS_00000 or CreditBureauResultCodes.SUCCESS_05000;
        var message = baseResponse?.resultMessage ?? wrappedResponse?.errorMessage ?? (isSuccess ? "Success" : "Unknown error");
        string? token = null;

        if (isSuccess)
        {
            Logger.LogInformation("LoanKey:{LoanKey} CI-{CiCode} success. Message:{Message}", loanKey, CiCode, message);
            LogWriter.Log(logFileName, $"LoanKey:{loanKey} CI-{CiCode:D3} success. Message:{message}");
        }
        else
        {
            Logger.LogError("LoanKey:{LoanKey} CI-{CiCode} error. Response:{Response}", loanKey, CiCode, response);
            LogWriter.Log(logFileName, $"LoanKey:{loanKey} CI-{CiCode:D3} error. Response:{response}");
        }

        return (isSuccess, message, token);
    }

    /// <summary>
    /// Форматирование даты для КАТМ
    /// </summary>
    protected static string FormatKatmDate(DateTimeOffset dateTime) => 
        dateTime.ToString("dd.MM.yyyy HH:mm:ss");

    /// <summary>
    /// Подготовка стандартного запроса
    /// </summary>
    protected BaseRequest<TRequest> CreateBaseRequest(TRequest request) => new()
    {
        Data = request,
        Security = RequestSecurity
    };

    /// <summary>
    /// Подготовка запроса с Header для кредитных заявок
    /// </summary>
    protected BaseRequestForCreditApplications<TRequest> CreateBaseRequestForCredit(TRequest request) => new()
    {
        Header = BankHeader,
        Request = request,
        Security = RequestSecurity
    };

    /// <summary>
    /// Установка стандартных полей PHead, PCode, PDate
    /// </summary>
    protected void SetStandardFields(object request, string? pDate = null)
    {
        if (request is not null)
        {
            var type = request.GetType();
            
            var pHeadProperty = type.GetProperty("PHead");
            var pCodeProperty = type.GetProperty("PCode");
            var pDateProperty = type.GetProperty("PDate");

            pHeadProperty?.SetValue(request, AsokiReportApiOptions.PHead);
            pCodeProperty?.SetValue(request, AsokiReportApiOptions.PCode);
            
            if (pDateProperty is not null)
            {
                var currentValue = pDateProperty.GetValue(request) as string;
                var newValue = string.IsNullOrWhiteSpace(currentValue)
                    ? (pDate ?? FormatKatmDate(DateTimeOffset.Now))
                    : currentValue.Trim();
                pDateProperty.SetValue(request, newValue);
            }
        }
    }
}
