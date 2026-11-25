using Application.Repositories.CreditBureauReportRepositories;
using CreditBureauService.Contracts.CreditBureauApplications.CreditRegistration.CreditAgreementsAndLeasing.Responses;
using CreditBureauService.Contracts.CreditBureauApplications.CreditRegistration.CreditApplications;
using CreditBureauService.Contracts.Common;
using Domain.Common.Constants;
using Domain.Common.Settings;
using Infrastructure.Common.Helpers.JsonHelpes;
using Infrastructure.Common.Helpers.Logger;
using Infrastructure.Services.HttpClients;
using Infrastructure.Services.Notifications;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using BankHeader =
    CreditBureauService.Contracts.CreditBureauApplications.CreditRegistration.CreditApplications.BankHeader;
using RequestSecurity = CreditBureauService.Contracts.Common.RequestSecurity;

namespace Infrastructure.Services.CreditBureauReportServices.Handlers;

/// <summary>
/// Базовый класс для обработчиков CI-запросов
/// </summary>
public abstract class CiHandlerBase<TRequest> : ICiHandler
{
    protected readonly ICreditBureauReportRepository CreditBureauReportRepository;
    protected readonly IRequestManagerService RequestManagerService;
    protected readonly CreditBureauReportApiOptions CreditBureauReportApiOptions;
    protected readonly CreditBureauApiOptions CreditBureauApiOptions;
    protected readonly RequestSecurity RequestSecurity;
    protected readonly BankHeader BankHeader;
    protected readonly LogWriter LogWriter;
    protected readonly ITelegramNotificationService TelegramNotificationService;
    protected readonly ILogger<CiHandlerBase<TRequest>> Logger;
    protected string? _currentRequestJson;

    protected CiHandlerBase(
        ICreditBureauReportRepository creditBureauReportRepository,
        IRequestManagerService requestManagerService,
        CreditBureauReportApiOptions CreditBureauReportApiOptions,
        CreditBureauApiOptions CreditBureauApiOptions,
        RequestSecurity requestSecurity,
        BankHeader bankHeader,
        LogWriter logWriter,
        ITelegramNotificationService telegramNotificationService,
        ILogger<CiHandlerBase<TRequest>> logger)
    {
        CreditBureauReportRepository = creditBureauReportRepository;
        RequestManagerService = requestManagerService;
        this.CreditBureauReportApiOptions = CreditBureauReportApiOptions;
        this.CreditBureauApiOptions = CreditBureauApiOptions;
        RequestSecurity = requestSecurity;
        BankHeader = bankHeader;
        LogWriter = logWriter;
        TelegramNotificationService = telegramNotificationService;
        Logger = logger;
    }

    public abstract int CiCode { get; }

    public abstract Task<CiProcessingResult> ProcessAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Универсальный метод обработки CI-запроса
    /// </summary>
    protected async Task<CiProcessingResult> ProcessCiRequestsAsync(
        Func<CancellationToken, Task<List<CreditBureauReportQueueItem<TRequest>>>> getRequestsFunc,
        Func<TRequest, object> prepareRequestFunc,
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
                var nullMessage = $"CI-{CiCode:D3} request is null";
                result.AddDetail(item.LoanKey, false, nullMessage);
                Logger.LogWarning("CI-{CiCode} skipped due to null request. LoanKey={LoanKey}", CiCode, item.LoanKey);
                await NotifyErrorAsync(
                    $"CI-{CiCode:D3} null request",
                    item.LoanKey,
                    category: "Пустой запрос",
                    reason: "В очереди CI запись есть, но тело запроса null.",
                    action: "Проверьте функцию очереди KATM_Report и данные заявки в БД.",
                    cancellationToken: cancellationToken);
                await CreditBureauReportRepository.UpsertCiStatusAsync(
                    item.LoanKey, CiCode, 2, nullMessage, null, cancellationToken);
                continue;
            }

            try
            {
                beforeRequestAction?.Invoke(item.Request);

                var baseRequest = prepareRequestFunc(item.Request);
                _currentRequestJson = baseRequest.ToJSON();
                LogFullRequest(item.LoanKey, _currentRequestJson);
                var response = await RequestManagerService.SendPostRequest(
                    endpoint,
                    _currentRequestJson,
                    item.LoanKey,
                    cancellationToken);
                LogFullResponse(item.LoanKey, response);

                if (string.IsNullOrWhiteSpace(response))
                {
                    result.Error++;
                    var emptyMessage = $"CI-{CiCode:D3} returned empty response";
                    result.AddDetail(item.LoanKey, false, emptyMessage);
                    Logger.LogError("CI-{CiCode} empty response. LoanKey={LoanKey}", CiCode, item.LoanKey);
                    await NotifyErrorAsync(
                        $"CI-{CiCode:D3} empty response",
                        item.LoanKey,
                        category: "Пустой ответ API",
                        reason: "КАТМ вернул пустой HTTP-ответ.",
                        action: "Проверьте доступность API КАТМ и логи RequestManager.",
                        cancellationToken: cancellationToken);
                    await CreditBureauReportRepository.UpsertCiStatusAsync(
                        item.LoanKey, CiCode, 2, emptyMessage, null, cancellationToken);
                    continue;
                }

                var (isSuccess, message, token) =
                    await ProcessResponseAsync(response, item.LoanKey, logFileName, cancellationToken);
                var ciStatus = (byte)(isSuccess ? 1 : 2);

                if (isSuccess)
                {
                    result.Success++;
                    result.AddDetail(item.LoanKey, true, message, response);
                }
                else
                {
                    result.Error++;
                    result.AddDetail(item.LoanKey, false, message, response);
                }

                await CreditBureauReportRepository.UpsertCiStatusAsync(
                    item.LoanKey, CiCode, ciStatus, message, token, cancellationToken);
            }
            catch (Exception ex)
            {
                result.Error++;
                var errorMessage = $"CI-{CiCode:D3} processing error: {ex.Message}";
                result.AddDetail(item.LoanKey, false, errorMessage);
                Logger.LogError(ex, "CI-{CiCode} error processing LoanKey={LoanKey}. Error={Error}", CiCode,
                    item.LoanKey, ex.Message);
                await NotifyExceptionAsync(
                    $"CI-{CiCode:D3} processing exception",
                    item.LoanKey,
                    ex,
                    context: $"Обработка CI-{CiCode:D3}",
                    cancellationToken);
                await CreditBureauReportRepository.UpsertCiStatusAsync(
                    item.LoanKey, CiCode, 2, errorMessage, null, cancellationToken);
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

    /// <summary>
    /// Обработка ответа от кредитного бюро
    /// </summary>
    protected virtual async Task<(bool IsSuccess, string Message, string? Token)> ProcessResponseAsync(
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
                category: "Неверный формат ответа",
                reason: "Ответ КАТМ не является JSON.",
                action: "Проверьте endpoint, Content-Type и тело ответа API.",
                details: null,
                responsePreview: GetResponsePreview(response, 1500),
                cancellationToken: cancellationToken);
            return (false, $"Invalid response from API: {GetResponsePreview(response, 200)}", null);
        }

        var wrappedResponse = TryDeserializeJson<BaseResponse<CreditRegistrationResponse>>(response);
        var baseResponse = wrappedResponse?.data ?? TryDeserializeJson<CreditRegistrationResponse>(response);
        if (baseResponse is null)
        {
            Logger.LogWarning("LoanKey:{LoanKey} CI-{CiCode} failed to deserialize response. Response:{Response}",
                loanKey, CiCode, GetResponsePreview(response, 500));
            await NotifyErrorAsync(
                $"CI-{CiCode:D3} invalid JSON structure",
                loanKey,
                category: "Неверная структура JSON",
                reason: "Не удалось разобрать ответ КАТМ в ожидаемую модель.",
                action: "Сверьте схему ответа с контрактом CI и логи Full.",
                details: null,
                responsePreview: GetResponsePreview(response, 1500),
                cancellationToken: cancellationToken);
            return (false, $"Invalid JSON structure from API: {GetResponsePreview(response, 200)}", null);
        }

        var isSuccess =
            baseResponse?.result is CreditBureauResultCodes.SUCCESS_00000 or CreditBureauResultCodes.SUCCESS_05000;
        var message = baseResponse?.resultMessage ??
                      wrappedResponse?.errorMessage ?? (isSuccess ? "Success" : "Unknown error");
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
            await NotifyErrorAsync(
                $"CI-{CiCode:D3} API error",
                loanKey,
                category: "Ошибка API КАТМ",
                reason: message,
                action: "Исправьте данные заявки или уточните код ошибки у КАТМ.",
                details: null,
                responsePreview: GetResponsePreview(response, 1500),
                cancellationToken: cancellationToken);
        }

        return (isSuccess, message, token);
    }

    protected bool IsJsonResponse(string? response)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            return false;
        }

        var trimmed = response.TrimStart();
        return trimmed.StartsWith('{') || trimmed.StartsWith('[');
    }

    protected string GetResponsePreview(string? response, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            return string.Empty;
        }

        var trimmed = response.Trim();
        return trimmed.Length > maxLength ? trimmed[..maxLength] + "..." : trimmed;
    }

    protected T? TryDeserializeJson<T>(string response) where T : class
    {
        try
        {
            return JsonConvert.DeserializeObject<T>(response);
        }
        catch (JsonException ex)
        {
            Logger.LogWarning(ex, "CI-{CiCode} JSON deserialization failed for type {TypeName}", CiCode,
                typeof(T).Name);
            return null;
        }
    }

    /// <summary>
    /// Полное логирование запроса CI в отдельный файл по фиче (CI{CiCode:D3}Full.txt)
    /// </summary>
    protected void LogFullRequest(int loanKey, string requestJson) =>
        LogWriter.Log($"CI{CiCode:D3}Full.txt", $"Type: CI-{CiCode:D3} Request\nLoanKey: {loanKey}\n{requestJson.RedactSecurity()}");

    /// <summary>
    /// Полное логирование ответа CI в отдельный файл по фиче (CI{CiCode:D3}Full.txt)
    /// </summary>
    protected void LogFullResponse(int loanKey, string response) =>
        LogWriter.Log($"CI{CiCode:D3}Full.txt", $"Type: CI-{CiCode:D3} Response\nLoanKey: {loanKey}\n{response}");

    protected Task NotifyErrorAsync(
        string source,
        int loanKey,
        string category,
        string reason,
        string action,
        CancellationToken cancellationToken = default) =>
        NotifyErrorAsync(source, loanKey, category, reason, action, details: null, responsePreview: null, cancellationToken);

    protected async Task NotifyErrorAsync(
        string source,
        int loanKey,
        string category,
        string reason,
        string action,
        string? details,
        string? responsePreview,
        CancellationToken cancellationToken = default)
    {
        if (TelegramNotificationService is null)
        {
            Logger.LogWarning("CI-{CiCode} TelegramNotificationService is null, skipping notification", CiCode);
            return;
        }

        var requestPreview = _currentRequestJson is not null
            ? GetResponsePreview(_currentRequestJson.RedactSecurity(), 1500)
            : null;

        var message = TelegramErrorFormatter.Format(
            category: category,
            reason: reason,
            action: action,
            context: $"CI-{CiCode:D3}",
            loanKey: loanKey.ToString(),
            details: details,
            request: requestPreview,
            response: responsePreview);

        var (app, customerId) = await CreditBureauReportRepository.GetLoanAppAndCustomerIdAsync(loanKey.ToString(), cancellationToken);

        await TelegramNotificationService.NotifyErrorAsync(
            source,
            message,
            app,
            customerId,
            cancellationToken);
    }

    protected async Task NotifyExceptionAsync(
        string source,
        int loanKey,
        Exception exception,
        string? context = null,
        CancellationToken cancellationToken = default)
    {
        if (TelegramNotificationService is null)
        {
            Logger.LogWarning("CI-{CiCode} TelegramNotificationService is null, skipping notification", CiCode);
            return;
        }

        var requestPreview = _currentRequestJson is not null
            ? GetResponsePreview(_currentRequestJson.RedactSecurity(), 1500)
            : null;

        var message = TelegramErrorFormatter.Format(
            exception,
            context: context ?? $"CI-{CiCode:D3}",
            loanKey: loanKey.ToString(),
            request: requestPreview);

        var (app, customerId) = await CreditBureauReportRepository.GetLoanAppAndCustomerIdAsync(loanKey.ToString(), cancellationToken);

        await TelegramNotificationService.NotifyErrorAsync(
            source,
            message,
            app,
            customerId,
            cancellationToken);
    }

    /// <summary>
    /// Форматирование даты для КАТМ
    /// </summary>
    protected static string FormatKatmDate(DateTimeOffset dateTime) =>
        dateTime.ToString("dd.MM.yyyy HH:mm:ss");

    protected static string FormatKatmIsoDateAtStartOfDay(DateTimeOffset dateTime)
    {
        return dateTime.Date.ToString("yyyy-MM-ddT00:00:00.000+0500");
    }

    /// <summary>
    /// Подготовка стандартного запроса
    /// </summary>
    protected BaseRequest<TRequest> CreateBaseRequest(TRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        return new BaseRequest<TRequest>
        {
            Data = request,
            Security = RequestSecurity ?? throw new InvalidOperationException("RequestSecurity is not configured")
        };
    }

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
    protected void SetStandardFields(object request, string? pDate = null, bool setPDate = true)
    {
        if (request is null)
        {
            Logger?.LogWarning("CI-{CiCode} SetStandardFields: request is null", CiCode);
            return;
        }

        if (CreditBureauReportApiOptions is null)
        {
            Logger?.LogError("CI-{CiCode} SetStandardFields: CreditBureauReportApiOptions is null", CiCode);
            return;
        }

        var type = request.GetType();

        var pHeadProperty = type.GetProperty("PHead");
        var pCodeProperty = type.GetProperty("PCode");
        var pDateProperty = type.GetProperty("PDate");

        pHeadProperty?.SetValue(request, CreditBureauReportApiOptions.PHead);
        pCodeProperty?.SetValue(request, CreditBureauReportApiOptions.PCode);

        if (setPDate && pDateProperty is not null)
        {
            var currentValue = pDateProperty.GetValue(request) as string;
            var newValue = string.IsNullOrWhiteSpace(currentValue)
                ? (pDate ?? FormatKatmDate(DateTimeOffset.Now))
                : currentValue.Trim();
            pDateProperty.SetValue(request, newValue);
        }
    }
}