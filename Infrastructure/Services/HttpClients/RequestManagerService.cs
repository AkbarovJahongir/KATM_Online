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
    public class RequestManagerService(LogWriter logWriter, ILogger<RequestManagerService> logger, IHelperRepository repository, IRequestManagerRepository requestManagerRepository, ITelegramNotificationService telegramNotificationService) : IRequestManagerService
    {
        private readonly LogWriter _logWriter = logWriter;
        private readonly ILogger<RequestManagerService> _logger = logger;
        private readonly IHelperRepository _repository = repository;
        private readonly IRequestManagerRepository _requestManagerRepository = requestManagerRepository;
        private readonly ITelegramNotificationService _telegramNotificationService = telegramNotificationService;
        
        private const int MaxRetries = 3;
        private const int InitialRetryDelayMs = 1000;

        public async Task<string> SendPostRequest(string url, string jsonData, string KeyLoanHistoryKb, IsXml isxml, CancellationToken cancellationToken)
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
            _logger.LogInformation("Sending POST request to External service API started...");
            _logger.LogInformation("JSON data sended to External service API is: " + jsonData);
            using var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(url);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var request = new HttpRequestMessage()
            {
                Content = new StringContent(jsonData, Encoding.UTF8, "application/json"),
                Method = HttpMethod.Post
            };

#pragma warning disable CS8602 // Разыменование вероятной пустой ссылки.
            request.Content.Headers.ContentType.CharSet = string.Empty;

            _logWriter.Log("RequestManager.txt", $"Key_RequestHistory:{KeyLoanHistoryKb} Request: {request.ToJSON()}");
            _logWriter.Log("RequestManager.txt", $"Key_RequestHistory:{KeyLoanHistoryKb} RequestBody: {jsonData}");
            _logger.LogInformation(message: $"POST request value: {request}");
            DateTime dateRequest = DateTime.Now;
            var httpResponseMessage = await httpClient.SendAsync(request, cancellationToken);
            DateTime dateResponse = DateTime.Now;
            var responseBody = httpResponseMessage.Content is null
                ? string.Empty
                : await httpResponseMessage.Content.ReadAsStringAsync(cancellationToken);

            _logWriter.Log("RequestManager.txt", $"Key_RequestHistory:{KeyLoanHistoryKb} Response: {httpResponseMessage.ToJSON()}");
            _logWriter.Log("RequestManager.txt", $"Key_RequestHistory:{KeyLoanHistoryKb} ResponseBody: {responseBody}");

            _logger.LogInformation(string.Format("POST Response status code is: {0}", httpResponseMessage.StatusCode));


            if (httpResponseMessage.StatusCode != HttpStatusCode.OK)
            {
                _logWriter.Log(
                    "RequestManager.txt",
                    $"Key_RequestHistory:{KeyLoanHistoryKb} FailedRequestBody: {jsonData}\nFailedResponseBody: {responseBody}");
                _logWriter.Log("RequestManager.txt", string.Format("Key_RequestHistory:{0} External service API send {1} status code! ResponseBody:{2}", KeyLoanHistoryKb, httpResponseMessage.StatusCode, responseBody));
                Console.WriteLine(
                    $"Key_RequestHistory:{KeyLoanHistoryKb} External service API send {httpResponseMessage.StatusCode} status code!\nRequestBody: {jsonData}\nResponseBody: {responseBody}");
                await _telegramNotificationService.NotifyErrorAsync(
                    "CI-017 request failed",
                    $"Key_RequestHistory: {KeyLoanHistoryKb}\nStatusCode: {(int)httpResponseMessage.StatusCode} ({httpResponseMessage.StatusCode})\nUrl: {url}\nRequestBody: {jsonData}\nResponseBody: {responseBody}",
                    cancellationToken);
                await _repository.KatmHelper(KeyLoanHistoryKb, string.Format("Key_LoanHistoryKb:{0} External service API send {1} status code!", KeyLoanHistoryKb, httpResponseMessage.StatusCode), IHelperRepository.TypeOperation.Error, cancellationToken);
                await _repository.KatmHelperXml(KeyLoanHistoryKb, string.Format("Key_LoanHistoryKb:{0} External service API send {1} status code!", KeyLoanHistoryKb, httpResponseMessage.StatusCode), IHelperRepository.TypeOperation.Error, cancellationToken);
                return result;
            }
            if (httpResponseMessage.Content == null)
            {
                _logWriter.Log("RequestManager.txt", string.Format("Key_RequestHistory:{0} Content object received from API is null!", KeyLoanHistoryKb));
                await _telegramNotificationService.NotifyErrorAsync(
                    "CI-017 empty response content",
                    $"Key_RequestHistory: {KeyLoanHistoryKb}\nUrl: {url}\nRequestBody: {jsonData}",
                    cancellationToken);
                await _repository.KatmHelper(KeyLoanHistoryKb, string.Format("Key_LoanHistoryKb:{0} Content object received from API is null!", KeyLoanHistoryKb), IHelperRepository.TypeOperation.Error, cancellationToken);
                await _repository.KatmHelperXml(KeyLoanHistoryKb, string.Format("Key_LoanHistoryKb:{0} Content object received from API is null!", KeyLoanHistoryKb), IHelperRepository.TypeOperation.Error, cancellationToken);
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

            _logger.LogInformation("LoanKey:{LoanKey}. Sending POST request started. Url:{Url}", LoanKey, url);
            _logger.LogInformation("LoanKey:{LoanKey}. RequestBody: {RequestBody}", LoanKey, jsonData);

            using var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(url);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var request = new HttpRequestMessage()
            {
                Content = new StringContent(jsonData, Encoding.UTF8, "application/json"),
                Method = HttpMethod.Post
            };

#pragma warning disable CS8602 // Разыменование вероятной пустой ссылки.
            request.Content.Headers.ContentType.CharSet = string.Empty;

            _logWriter.Log("RequestManager.txt", $"LoanKey:{LoanKey}. Request: {request.ToJSON()}");
            _logWriter.Log("RequestManager.txt", $"LoanKey:{LoanKey}. RequestBody: {jsonData}");
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
                        httpResponseMessage = await httpClient.SendAsync(request, cancellationToken);
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
                    catch (HttpRequestException ex) when (attempt < MaxRetries)
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
                await _telegramNotificationService.NotifyErrorAsync(
                    "RequestManagerService POST request failed",
                    $"LoanKey: {LoanKey}\nUrl: {url}\nMessage: {ex.Message}\nException: {ex}",
                    cancellationToken);

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
        }
    }
}
