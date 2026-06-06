using Application.Repositories.CreditBureauReportRepositories;
using Application.Repositories.Helpers;
using Application.Repositories.RequestManager;
using CreditBureauService.Contracts.CreditBureauApplications;
using CreditBureauService.Contracts.CreditBureauApplications.CreditReports;
using CreditBureauService.Contracts.Common;
using Domain.Common.Constants;
using Domain.Common.Settings;
using Infrastructure.Common.Helpers.JsonHelpes;
using Infrastructure.Common.Helpers.Logger;
using Infrastructure.CreditReportsXml.Parsers;
using Infrastructure.Services.HttpClients;
using Infrastructure.Services.Notifications;
using Newtonsoft.Json;

namespace Infrastructure.CreditReportsXml
{
    public class CreditReportXmlService(
        IRequestManagerService requestManagerService,
        CreditBureauReportApiOptions options,
        RequestSecurity requestSecurity,
        LogWriter logWriter,
        IHelperRepository helperRepository,
        ICreditReportXmlParser creditReportXmlParser,
        IRequestManagerRepository requestManagerRepository,
        ICreditBureauReportRepository creditBureauReportRepository,
        ITelegramNotificationService telegramNotificationService) : ICreditReportXmlService
    {
        private readonly IHelperRepository _helperRepository = helperRepository;
        private readonly ICreditReportXmlParser _creditReportXmlParser = creditReportXmlParser;
        private readonly IRequestManagerService _requestManagerService = requestManagerService;
        private readonly CreditBureauReportApiOptions _options = options;
        private readonly RequestSecurity _requestSecurity = requestSecurity;
        private readonly LogWriter _logWriter = logWriter;
        private readonly IRequestManagerRepository _requestManagerRepository = requestManagerRepository;
        private readonly ICreditBureauReportRepository _creditBureauReportRepository = creditBureauReportRepository;
        private readonly ITelegramNotificationService _telegramNotificationService = telegramNotificationService;
        private const string CreditReport017FullLogFile = "CreditReport017Full.txt";
        public async Task CreditReportXml(LoanApplication loanApplications, CancellationToken cancellationToken)
        {
            try
            {
                // подготавливаем запрос
                var creditReportRequest = new CreditReportRequest()
                {
                    PClaimId = loanApplications.PClaimId,
                    PLoanSubject = loanApplications.PLoanSubject,
                    PLoanSubjectType = loanApplications.PLoanSubjectType,
                    PPin = loanApplications.PPin,
                    PReportId = loanApplications.PReportId,
                    PTin = loanApplications.PTin,
                    PHead = _options.PHead,
                    PCode = _options.PCode,
                    PReportFormat = 0,
                    PReportReason = loanApplications.PReportReason
                };
                var request = new BaseRequest<CreditReportRequest>() { Data = creditReportRequest, Security = _requestSecurity };
                var requestJson = request.ToJSON();
                Console.WriteLine($"CI-017 XML Request. LoanKey:{loanApplications.KeyCreditBureauKb} ClaimId:{loanApplications.PClaimId}\n{requestJson}");
                _logWriter.Log(
                    CreditReport017FullLogFile,
                    $"Type: CI-017 XML Request\nKeyLoanHistoryKb: {loanApplications.KeyCreditBureauKb}\nClaimId: {loanApplications.PClaimId}\n{requestJson}");

                // Отправляем запрос
                var dateRequest = DateTime.Now;
                var response = await _requestManagerService.SendPostRequest(
                    _options.HostAddress + _options.ReportUrl,
                    requestJson,
                    loanApplications.KeyCreditBureauKb,
                    IRequestManagerRepository.IsXml.Xml,
                    cancellationToken);
                var dateResponse = DateTime.Now;

                await _requestManagerRepository.InsertRequestLog(
                    _options.HostAddress + _options.ReportUrl,
                    requestJson,
                    "POST",
                    string.IsNullOrWhiteSpace(response) ? "0" : "200",
                    response,
                    dateRequest,
                    dateResponse,
                    loanApplications.KeyCreditBureauKb,
                    cancellationToken);

                _logWriter.Log(
                    CreditReport017FullLogFile,
                    $"Type: CI-017 XML Response\nKeyLoanHistoryKb: {loanApplications.KeyCreditBureauKb}\nClaimId: {loanApplications.PClaimId}\n{response}");
                if (string.IsNullOrWhiteSpace(response))
                {
                    await NotifyErrorAsync("CI-017 XML empty response", loanApplications, "API вернул пустой ответ", cancellationToken);
                    return;
                }
                var baseResponse = JsonConvert.DeserializeObject<BaseResponse<CreditReportResponse>>(response);
                _logWriter.Log("CreditReportResponseXml.txt", $"KeyAbsLoan:ClaimId: {loanApplications.PClaimId} - KeyRequestHistoryKb:{loanApplications.KeyCreditBureauKb} - {DateTime.Now}\n\n" + baseResponse?.ToJSON());
                // Проверяем запрос
                // Код ответа(05000 - успешно)
                if (baseResponse?.data?.result == CreditBureauResultCodes.SUCCESS_05000)
                {
                    // Сохраняем в базу данных Base64
                    try
                    {
                        await _helperRepository.KatmHelperXml(loanApplications.KeyCreditBureauKb, baseResponse.data.reportBase64, IHelperRepository.TypeOperation.Base64, cancellationToken);
                        _logWriter.Log("TestParser.txt", "start");
                        try
                        {
                            await _creditReportXmlParser.ParseAndPersistAsync(loanApplications.KeyCreditBureauKb, baseResponse.data.reportBase64, cancellationToken);
                            _logWriter.Log("TestParser.txt", "Успех");
                        }
                        catch (Exception ex)
                        {
                            _logWriter.Log("TestParser.txt", "catch 1--" + ex.Message);
                        }
                        await _creditBureauReportRepository.UpsertCiStatusAsync(int.Parse(loanApplications.KeyCreditBureauKb), 17, 1, "Success", null, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logWriter.Log("SaveReportBase64Xml.txt", baseResponse.data.reportBase64 + "\n\n" + ex.Message);
                        return;
                    }
                }
                // При получении ошибки (result = 05050) необходимо через короткие интервалы (не менее 60 секунд)
                // проверять статус кредитного отчёта по рауту /credit/report/status
                else if (baseResponse?.data?.result == CreditBureauResultCodes.WAIT_AND_TRY_AGAIN)
                {
                    // Проверяем токен если токен существует то сохраняем в файл
                    if (!string.IsNullOrEmpty(baseResponse.data.token))
                    {
                        // сохраняем токен
                        await _helperRepository.KatmHelperXml(loanApplications.KeyCreditBureauKb, baseResponse.data.token, IHelperRepository.TypeOperation.Token, cancellationToken);
                        await _creditBureauReportRepository.UpsertCiStatusAsync(int.Parse(loanApplications.KeyCreditBureauKb), 17, 0, "Waiting", baseResponse.data.token, cancellationToken);
                        return;
                    }
                }
                // Claim not found - Заявка не найдена
                else if (baseResponse?.data?.result == CreditBureauResultCodes.NO_TOKEN_FOUND)
                {
                    if (string.IsNullOrEmpty(baseResponse.data.token))
                    {
                        await _helperRepository.KatmHelperXml(loanApplications.KeyCreditBureauKb, "Заявка не найдена!", IHelperRepository.TypeOperation.Error, cancellationToken);
                        await _creditBureauReportRepository.UpsertCiStatusAsync(int.Parse(loanApplications.KeyCreditBureauKb), 17, 2, "Claim not found", null, cancellationToken);
                        await NotifyErrorAsync("CI-017 XML API error", loanApplications, $"Message: Заявка не найдена\nResult: {baseResponse.data.result}", cancellationToken);
                        return;
                    }
                }
                else if (baseResponse?.data?.result == CreditBureauResultCodes.IDENTICAL_REQUEST)
                {
                    await _helperRepository.KatmHelperXml(loanApplications.KeyCreditBureauKb, 5.ToJSON(), IHelperRepository.TypeOperation.AddNextAccess, cancellationToken);
                    await _creditBureauReportRepository.UpsertCiStatusAsync(int.Parse(loanApplications.KeyCreditBureauKb), 17, 2, "Identical request", null, cancellationToken);
                    await NotifyErrorAsync("CI-017 XML API error", loanApplications, $"Message: Идентичный запрос\nResult: {baseResponse.data.result}", cancellationToken);
                }
                else if (baseResponse?.data?.result == CreditBureauResultCodes.FREEZE_SERVICE_ACTIVE)
                {
                    await _helperRepository.KatmHelperXml(loanApplications.KeyCreditBureauKb, "Субъект не дает согласия на получение кредитной истории, подключена услуга Freeze. Субъекту необходимо отключить услугу Freeze.", IHelperRepository.TypeOperation.Error, cancellationToken);
                    await _creditBureauReportRepository.UpsertCiStatusAsync(int.Parse(loanApplications.KeyCreditBureauKb), 17, 2, "Freeze service active", null, cancellationToken);
                    await NotifyErrorAsync("CI-017 XML API error", loanApplications, $"Message: Freeze service active\nResult: {baseResponse.data.result}", cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logWriter.Log("CreditReportCatchXml.txt", $"KeyAbsLoan:ClaimId: {loanApplications.PClaimId} - KeyRequestHistoryKb:{loanApplications.KeyCreditBureauKb} - {DateTime.Now}\n\n" + ex.Message);
                await NotifyErrorAsync("CI-017 XML processing exception", loanApplications, $"Message: {ex.Message}\nStackTrace: {ex.StackTrace}", cancellationToken);
                return;
            }
        }
        public async Task CreditReportStatusXml(LoanApplication loanApplications, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var creditReportStatusRequest = new CreditReportStatusRequest
                    {
                        pHead = _options.PHead,
                        pCode = _options.PCode,
                        pReportFormat = 0,
                        pClaimId = loanApplications.PClaimId,
                        pToken = loanApplications.PToken!
                    };
                    var request = new BaseRequest<CreditReportStatusRequest>() { Data = creditReportStatusRequest, Security = _requestSecurity };
                    var requestJson = request.ToJSON();
                    _logWriter.Log("CreditReportStatusRequestXml.txt", $"KeyAbsLoan:ClaimId: {loanApplications.PClaimId} - KeyRequestHistoryKb:{loanApplications.KeyCreditBureauKb} - {DateTime.Now}\n\n" + requestJson);
                    Console.WriteLine($"CI-017 XML Status Request. LoanKey:{loanApplications.KeyCreditBureauKb} ClaimId:{loanApplications.PClaimId}\n{requestJson}");
                    _logWriter.Log(
                        CreditReport017FullLogFile,
                        $"Type: CI-017 XML Status Request\nKeyLoanHistoryKb: {loanApplications.KeyCreditBureauKb}\nClaimId: {loanApplications.PClaimId}\n{requestJson}");

                    var dateRequest = DateTime.Now;
                    var response = await _requestManagerService.SendPostRequest(
                        _options.HostAddress + _options.ReportStatusUrl,
                        requestJson,
                        loanApplications.KeyCreditBureauKb,
                        IRequestManagerRepository.IsXml.Xml,
                        cancellationToken);
                    var dateResponse = DateTime.Now;

                    await _requestManagerRepository.InsertRequestLog(
                        _options.HostAddress + _options.ReportStatusUrl,
                        requestJson,
                        "POST",
                        string.IsNullOrWhiteSpace(response) ? "0" : "200",
                        response,
                        dateRequest,
                        dateResponse,
                        loanApplications.KeyCreditBureauKb,
                        cancellationToken);

                    _logWriter.Log(
                        CreditReport017FullLogFile,
                        $"Type: CI-017 XML Status Response\nKeyLoanHistoryKb: {loanApplications.KeyCreditBureauKb}\nClaimId: {loanApplications.PClaimId}\n{response}");
                    if (string.IsNullOrWhiteSpace(response))
                    {
                        await NotifyErrorAsync("CI-017 XML status empty response", loanApplications, "API вернул пустой ответ при проверке статуса", cancellationToken);
                        return;
                    }

                    var baseResponse = JsonConvert.DeserializeObject<BaseResponse<CreditReportStatusResponse>>(response);
                    _logWriter.Log("CreditReportStatusResponseXml.txt", $"KeyAbsLoan:ClaimId: {loanApplications.PClaimId} - KeyRequestHistoryKb:{loanApplications.KeyCreditBureauKb} - {DateTime.Now}\n\n" + baseResponse?.ToJSON());

                    if (baseResponse.data.result == CreditBureauResultCodes.SUCCESS_05000)
                    {
                        await _helperRepository.KatmHelperXml(loanApplications.KeyCreditBureauKb, baseResponse.data.reportBase64, IHelperRepository.TypeOperation.Base64, cancellationToken);
                        _logWriter.Log("TestParser.txt", "start");
                        try
                        {
                            await _creditReportXmlParser.ParseAndPersistAsync(loanApplications.KeyCreditBureauKb, baseResponse.data.reportBase64, cancellationToken);
                            _logWriter.Log("TestParser.txt", "Успех");
                        }
                        catch (Exception ex)
                        {
                            _logWriter.Log("TestParser.txt", "catch 1--" + ex.Message);
                        }
                        await _creditBureauReportRepository.UpsertCiStatusAsync(int.Parse(loanApplications.KeyCreditBureauKb), 17, 1, "Success", null, cancellationToken);
                        return;
                    }
                    else if (baseResponse.data.result == CreditBureauResultCodes.IDENTICAL_REQUEST)
                    {
                        await _helperRepository.KatmHelperXml(loanApplications.KeyCreditBureauKb, 5.ToJSON(), IHelperRepository.TypeOperation.AddNextAccess, cancellationToken);
                        await _creditBureauReportRepository.UpsertCiStatusAsync(int.Parse(loanApplications.KeyCreditBureauKb), 17, 2, "Identical request", null, cancellationToken);
                        return;
                    }
                    else if (baseResponse.data.result == CreditBureauResultCodes.WAIT_AND_TRY_AGAIN)
                    {
                        await _helperRepository.KatmHelperXml(loanApplications.KeyCreditBureauKb, 1.ToJSON(), IHelperRepository.TypeOperation.AddNextAccess, cancellationToken);
                        await _creditBureauReportRepository.UpsertCiStatusAsync(int.Parse(loanApplications.KeyCreditBureauKb), 17, 0, "Waiting", loanApplications.PToken, cancellationToken);
                        await Task.Delay(_options.CheckReportStatusInterval, cancellationToken);
                    }
                    else
                    {
                        return;
                    }
                }
                catch (Exception ex)
                {
                    _logWriter.Log("CreditReportStatusResponseXml.txt", $"KeyAbsLoan:ClaimId: {loanApplications.PClaimId} - KeyRequestHistoryKb:{loanApplications.KeyCreditBureauKb} - {DateTime.Now}\n\n" + ex.Message);
                    await NotifyErrorAsync("CI-017 XML status processing exception", loanApplications, $"Message: {ex.Message}\nStackTrace: {ex.StackTrace}", cancellationToken);
                    return;
                }
            }
        }

        private async Task NotifyErrorAsync(string source, LoanApplication loan, string details, CancellationToken cancellationToken)
        {
            var (app, customerId) = await _creditBureauReportRepository.GetLoanAppAndCustomerIdAsync(
                int.Parse(loan.KeyCreditBureauKb), cancellationToken);

            await _telegramNotificationService.NotifyErrorAsync(
                source,
                $"LoanKey: {loan.KeyCreditBureauKb}\nClaimId: {loan.PClaimId}\nDetails: {details}",
                app,
                customerId,
                cancellationToken);
        }
    }
}
