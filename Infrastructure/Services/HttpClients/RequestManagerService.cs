using Application.Repositories.CreditBureauReportRepositories;
using Application.Repositories.Helpers;
using Application.Repositories.RequestManager;
using Infrastructure.Common.Helpers.JsonHelpes;
using Infrastructure.Common.Helpers.Logger;
using Infrastructure.Services.Notifications;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using static Application.Repositories.RequestManager.IRequestManagerRepository;

namespace Infrastructure.Services.HttpClients
{
    public class RequestManagerService(LogWriter logWriter, ILogger<RequestManagerService> logger, IHelperRepository repository, IRequestManagerRepository requestManagerRepository, ITelegramNotificationService telegramNotificationService, ICreditBureauReportRepository creditBureauReportRepository, IKatmAuthService katmAuthService) : IRequestManagerService
    {
        private readonly LogWriter _logWriter = logWriter;
        private readonly ILogger<RequestManagerService> _logger = logger;
        private readonly IHelperRepository _repository = repository;
        private readonly IRequestManagerRepository _requestManagerRepository = requestManagerRepository;
        private readonly ITelegramNotificationService _telegramNotificationService = telegramNotificationService;
        private readonly ICreditBureauReportRepository _creditBureauReportRepository = creditBureauReportRepository;
        private readonly IKatmAuthService _katmAuthService = katmAuthService;
        
        private const int MaxRetries = 3;
        private const int InitialRetryDelayMs = 1000;
        private static readonly TimeSpan HttpRequestTimeout = TimeSpan.FromSeconds(60);
        private const string CreditReport017FullLogFile = "CreditReport017Full.txt";

        private static HttpClient CreateHttpClient(string url)
        {
            var httpClient = new HttpClient
            {
                BaseAddress = new Uri(url),
                Timeout = HttpRequestTimeout
            };
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return httpClient;
        }

        public async Task<string> SendPostRequest(string url, string jsonData, string KeyLoanHistoryKb, IsXml isxml, CancellationToken cancellationToken, string? requestSubtype = null)
        {
            string result = string.Empty;
            if (string.IsNullOrWhiteSpace(url))
            {
                _logWriter.Log("RequestManager.txt", "Couldn't send request to External service API. \"URL\" parameter passed to \"SendPostRequest\" method is null or empty!");
                return result;
            }
            if (string.IsNullOrWhiteSpace(jsonData))
            {
                _logWriter.Log("RequestManager.txt", "Couldn't send request to External service API. \"jsonData\" parameter passed to \"SendPostRequest\" method is null or empty!");
                return result;
            }
            var redactedJsonData = jsonData.RedactSecurity();

            _logger.LogInformation("Sending POST request to External service API started...");
            _logger.LogInformation("JSON data sended to External service API is: " + redactedJsonData);
            using var httpClient = CreateHttpClient(url);

            var request = new HttpRequestMessage()
            {
                Content = new StringContent(jsonData, Encoding.UTF8, "application/json"),
                Method = HttpMethod.Post
            };

#pragma warning disable CS8602 // Разыменование вероятной пустой ссылки.
            request.Content.Headers.ContentType.CharSet = string.Empty;

            _logWriter.Log("RequestManager.txt", $"Key_RequestHistory:{KeyLoanHistoryKb} Request: {request.ToJSON()}");
            _logWriter.Log("RequestManager.txt", $"Key_RequestHistory:{KeyLoanHistoryKb} RequestBody: {redactedJsonData}");
            _logger.LogInformation(message: $"POST request value: {request}");
            DateTime dateRequest = DateTime.Now;
            HttpResponseMessage httpResponseMessage;
            string responseBody;
            try
            {
                // request ниже используется только для логов — в нём нет заголовка с токеном.
                httpResponseMessage = await _katmAuthService.SendPostAsync(httpClient, jsonData, cancellationToken);
                responseBody = httpResponseMessage.Content is null
                    ? string.Empty
                    : await httpResponseMessage.Content.ReadAsStringAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                DateTime dateResponseEx = DateTime.Now;
                _logWriter.Log("RequestManager.txt", $"Key_RequestHistory:{KeyLoanHistoryKb} POST request threw exception: {ex.Message}");
                _logWriter.Log(CreditReport017FullLogFile, $"Type: CI-017 Exception\nKeyLoanHistoryKb: {KeyLoanHistoryKb}\nUrl: {url}\nRequestBody: {redactedJsonData}\n{ex}");
                var (appEx, customerIdEx) = await FetchAppAndCustomerId(KeyLoanHistoryKb, cancellationToken);
                await _telegramNotificationService.NotifyErrorAsync(
                    "CI-017 request exception",
                    TelegramErrorFormatter.Format(
                        ex,
                        context: "CI-017 POST к API КАТМ",
                        loanKey: KeyLoanHistoryKb,
                        url: url,
                        request: redactedJsonData),
                    appEx, customerIdEx, cancellationToken);
                var (codeEx, messageEx) = await _requestManagerRepository.InsertLog(KeyLoanHistoryKb, url, jsonData, request.Method.Method, 0, ex.Message, dateRequest, dateResponseEx, isxml, cancellationToken);
                if (string.IsNullOrWhiteSpace(codeEx) || codeEx == "1")
                    _logWriter.EmergencyLog("EmergencyLog.txt", "Key_RequestHistory:" + KeyLoanHistoryKb + "\n\nException:" + ex.Message + "\n\n" + messageEx);
                if (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var comment = $"Не удалось отправить запрос CI-017 для отчёта {requestSubtype}. Подробности записаны в журнале сервиса.";
                        if (isxml == IsXml.Xml)
                            await _repository.KatmHelperXml(KeyLoanHistoryKb, comment, IHelperRepository.TypeOperation.Comment, cancellationToken, requestSubtype);
                        else
                            await _repository.KatmHelper(KeyLoanHistoryKb, comment, IHelperRepository.TypeOperation.Comment, cancellationToken, requestSubtype);
                    }
                    catch (Exception commentException)
                    {
                        _logWriter.Log("RequestManager.txt", $"Failed to write Loan_History_KB comment for {KeyLoanHistoryKb}: {commentException.Message}");
                    }
                }
                return result;
            }
            DateTime dateResponse = DateTime.Now;

            _logWriter.Log("RequestManager.txt", $"Key_RequestHistory:{KeyLoanHistoryKb} Response: {httpResponseMessage.ToJSON()}");
            _logWriter.Log("RequestManager.txt", $"Key_RequestHistory:{KeyLoanHistoryKb} ResponseBody: {responseBody}");

            _logger.LogInformation(string.Format("POST Response status code is: {0}", httpResponseMessage.StatusCode));


            if (httpResponseMessage.StatusCode != HttpStatusCode.OK)
            {
                _logWriter.Log(
                    "RequestManager.txt",
                    $"Key_RequestHistory:{KeyLoanHistoryKb} FailedRequestBody: {redactedJsonData}\nFailedResponseBody: {responseBody}");
                _logWriter.Log("RequestManager.txt", string.Format("Key_RequestHistory:{0} External service API send {1} status code! ResponseBody:{2}", KeyLoanHistoryKb, httpResponseMessage.StatusCode, responseBody));
                Console.WriteLine(
                    $"Key_RequestHistory:{KeyLoanHistoryKb} External service API send {httpResponseMessage.StatusCode} status code!\nRequestBody: {redactedJsonData}\nResponseBody: {responseBody}");
                var (app1, customerId1) = await FetchAppAndCustomerId(KeyLoanHistoryKb, cancellationToken);
                await _telegramNotificationService.NotifyErrorAsync(
                    "CI-017 request failed",
                    TelegramErrorFormatter.Format(
                        category: "HTTP ошибка КАТМ",
                        reason: $"API вернул статус {(int)httpResponseMessage.StatusCode} ({httpResponseMessage.StatusCode}).",
                        action: "Проверьте доступность API, тело запроса и ответ КАТМ.",
                        context: "CI-017 POST",
                        loanKey: KeyLoanHistoryKb,
                        url: url,
                        statusCode: $"{(int)httpResponseMessage.StatusCode} ({httpResponseMessage.StatusCode})",
                        request: redactedJsonData,
                        response: responseBody),
                    app1, customerId1, cancellationToken);
                var error = $"КАТМ вернул HTTP-статус {(int)httpResponseMessage.StatusCode} при запросе CI-017 для отчёта {requestSubtype}. Подробности записаны в журнале сервиса.";
                if (isxml == IsXml.Xml)
                    await _repository.KatmHelperXml(KeyLoanHistoryKb, error, IHelperRepository.TypeOperation.Error, cancellationToken, requestSubtype);
                else
                    await _repository.KatmHelper(KeyLoanHistoryKb, error, IHelperRepository.TypeOperation.Error, cancellationToken, requestSubtype);
                return result;
            }
            if (httpResponseMessage.Content == null)
            {
                _logWriter.Log("RequestManager.txt", string.Format("Key_RequestHistory:{0} Content object received from API is null!", KeyLoanHistoryKb));
                var (app2, customerId2) = await FetchAppAndCustomerId(KeyLoanHistoryKb, cancellationToken);
                await _telegramNotificationService.NotifyErrorAsync(
                    "CI-017 empty response content",
                    TelegramErrorFormatter.Format(
                        category: "Пустой ответ API",
                        reason: "HTTP-ответ без Content (null).",
                        action: "Проверьте доступность API КАТМ и сетевой путь.",
                        context: "CI-017 POST",
                        loanKey: KeyLoanHistoryKb,
                        url: url,
                        request: redactedJsonData),
                    app2, customerId2, cancellationToken);
                var error = $"КАТМ вернул пустой ответ на запрос CI-017 для отчёта {requestSubtype}.";
                if (isxml == IsXml.Xml)
                    await _repository.KatmHelperXml(KeyLoanHistoryKb, error, IHelperRepository.TypeOperation.Error, cancellationToken, requestSubtype);
                else
                    await _repository.KatmHelper(KeyLoanHistoryKb, error, IHelperRepository.TypeOperation.Error, cancellationToken, requestSubtype);
                return result;
            }
            _logger.LogInformation(string.Format("POST Response value: {0}", httpResponseMessage));

            string str2 = responseBody;

            _logger.LogInformation("Content value received from External service API is " + str2);
            _logger.LogInformation("POST REQUEST FINISHED!");
            result = str2;
            //Insert Logs
            var (code, message) = await _requestManagerRepository.InsertLog(KeyLoanHistoryKb, url, jsonData, request.Method.Method, (int)httpResponseMessage.StatusCode, result, dateRequest, dateResponse, isxml, cancellationToken);
            if (string.IsNullOrWhiteSpace(code) || code == "1")
                _logWriter.EmergencyLog("EmergencyLog.txt", "Key_RequestHistory:" + KeyLoanHistoryKb + "header:" + httpResponseMessage.Headers + "\n\n" + "Body" + str2 + "\n\n" + message);
            return result;
        }

        public async Task<string> SendPostRequest(string url, string jsonData, int LoanKey, CancellationToken cancellationToken)
        {
            string result = string.Empty;
            var loanKeyText = LoanKey.ToString();

            if (string.IsNullOrWhiteSpace(url))
            {
                _logWriter.Log("RequestManager.txt", $"LoanKey:{LoanKey}. URL is null or empty in SendPostRequest(int LoanKey).");
                return result;
            }
            if (string.IsNullOrWhiteSpace(jsonData))
            {
                _logWriter.Log("RequestManager.txt", $"LoanKey:{LoanKey}. jsonData is null or empty in SendPostRequest(int LoanKey).");
                return result;
            }

            var redactedJsonData = jsonData.RedactSecurity();

            _logger.LogInformation("LoanKey:{LoanKey}. Sending POST request started. Url:{Url}", LoanKey, url);
            _logger.LogInformation("LoanKey:{LoanKey}. RequestBody: {RequestBody}", LoanKey, redactedJsonData);

            using var httpClient = CreateHttpClient(url);

            HttpRequestMessage request = new()
            {
                Content = new StringContent(jsonData, Encoding.UTF8, "application/json"),
                Method = HttpMethod.Post
            };

#pragma warning disable CS8602 // Разыменование вероятной пустой ссылки.
            request.Content.Headers.ContentType.CharSet = string.Empty;

            _logWriter.Log("RequestManager.txt", $"LoanKey:{LoanKey}. Request: {request.ToJSON()}");
            _logWriter.Log("RequestManager.txt", $"LoanKey:{LoanKey}. RequestBody: {redactedJsonData}");
            DateTime dateRequest = DateTime.Now;
            HttpResponseMessage? httpResponseMessage = null;
            string responseBody = string.Empty;
            Exception? lastException = null;

            try
            {
                for (int attempt = 1; attempt <= MaxRetries; attempt++)
                {
                    try
                    {
                        httpResponseMessage = await _katmAuthService.SendPostAsync(httpClient, jsonData, cancellationToken);
                        DateTime dateResponse = DateTime.Now;
                        responseBody = httpResponseMessage.Content is null
                            ? string.Empty
                            : await httpResponseMessage.Content.ReadAsStringAsync(cancellationToken);

                        _logger.LogInformation(
                            "LoanKey:{LoanKey}. POST finished with status {StatusCode}",
                            LoanKey,
                            (int)httpResponseMessage.StatusCode);
                        _logger.LogDebug("LoanKey:{LoanKey}. Response body: {ResponseBody}", LoanKey, responseBody);

                        var contentType = httpResponseMessage.Content.Headers.ContentType?.MediaType;
                        if (contentType != "application/json")
                        {
                            _logger.LogWarning(
                                "LoanKey:{LoanKey}. Unexpected Content-Type: {ContentType}. Response: {ResponseBody}",
                                LoanKey, contentType, responseBody);
                            _logWriter.Log(
                                "RequestManager.txt",
                                $"LoanKey:{LoanKey}. WARNING: Content-Type is {contentType}, expected application/json. ResponseBody:{responseBody}");
                        }

                        _logWriter.Log(
                            "RequestManager.txt",
                            $"LoanKey:{LoanKey}. ResponseStatus:{(int)httpResponseMessage.StatusCode}. ResponseBody:{responseBody}");

                        var (code, message) = await _requestManagerRepository.InsertRequestLog(
                            url,
                            jsonData,
                            request.Method.Method,
                            ((int)httpResponseMessage.StatusCode).ToString(),
                            responseBody,
                            dateRequest,
                            dateResponse,
                            LoanKey.ToString(),
                            cancellationToken);

                        if (string.IsNullOrWhiteSpace(code) || code == "1")
                        {
                            _logWriter.EmergencyLog(
                                "EmergencyLog.txt",
                                $"LoanKey:{loanKeyText}. Failed to write RequestLogs. Message:{message}. Status:{(int)httpResponseMessage.StatusCode}");
                        }

                        result = responseBody;
                        return result;
                    }
                    catch (Exception ex) when (
                        attempt < MaxRetries &&
                        !cancellationToken.IsCancellationRequested &&
                        (ex is HttpRequestException || ex is TaskCanceledException))
                    {
                        lastException = ex;
                        var delay = InitialRetryDelayMs * (int)Math.Pow(2, attempt - 1);
                        _logger.LogWarning("LoanKey:{LoanKey}. Attempt {Attempt}/{MaxRetries} failed. Retrying in {Delay}ms. Error: {Error}", LoanKey, attempt, MaxRetries, delay, ex.Message);
                        _logWriter.Log("RequestManager.txt", $"LoanKey:{LoanKey}. Attempt {attempt} failed: {ex.Message}. Retrying...");
                        await Task.Delay(delay, cancellationToken);
                    }
                }
                return result;
            }
            catch (Exception ex) when (ex != lastException)
            {
                DateTime dateResponse = DateTime.Now;
                _logger.LogError(ex, "LoanKey:{LoanKey}. POST request failed", LoanKey);
                _logWriter.Log("RequestManager.txt", $"LoanKey:{LoanKey}. POST request failed: {ex.Message}");
                var (app3, customerId3) = await _creditBureauReportRepository.GetLoanAppAndCustomerIdAsync(LoanKey.ToString(), cancellationToken);
                await _telegramNotificationService.NotifyErrorAsync(
                    "RequestManagerService POST request failed",
                    TelegramErrorFormatter.Format(
                        ex,
                        context: "RequestManagerService.SendPostRequest",
                        loanKey: LoanKey.ToString(),
                        url: url,
                        request: redactedJsonData),
                    app3, customerId3, cancellationToken);

                var (code, message) = await _requestManagerRepository.InsertRequestLog(
                    url,
                    jsonData,
                    request.Method.Method,
                    httpResponseMessage is null ? "0" : ((int)httpResponseMessage.StatusCode).ToString(),
                    string.IsNullOrWhiteSpace(responseBody) ? ex.Message : responseBody,
                    dateRequest,
                    dateResponse,
                    LoanKey.ToString(),
                    cancellationToken);

                if (string.IsNullOrWhiteSpace(code) || code == "1")
                {
                    _logWriter.EmergencyLog(
                        "EmergencyLog.txt",
                        $"LoanKey:{loanKeyText}. Failed to write RequestLogs after exception. Message:{message}");
                }

                return string.Empty;
            }
            finally
            {
                request.Dispose();
            }
        }

        private async Task<(string? App, string? CustomerId)> FetchAppAndCustomerId(string keyLoanHistoryKb, CancellationToken cancellationToken)
        {
            return await _creditBureauReportRepository.GetLoanAppAndCustomerIdAsync(keyLoanHistoryKb, cancellationToken);
        }
    }
}
