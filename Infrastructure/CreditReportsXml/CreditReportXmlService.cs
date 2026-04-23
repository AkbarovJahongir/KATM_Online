using Application.Repositories.Helpers;
using Application.Repositories.RequestManager;
using CreditBureau.Contracts.AsokiLoanApplications;
using CreditBureau.Contracts.AsokiLoanApplications.СreditReports;
using CreditBureau.Contracts.Common;
using Domain.Common.Constants;
using Domain.Common.Settings;
using Infrastructure.Common.Helpers.JsonHelpes;
using Infrastructure.Common.Helpers.Logger;
using Infrastructure.CreditReportsXml.Parsers;
using Infrastructure.Services.HttpClients;
using Newtonsoft.Json;

namespace Infrastructure.CreditReportsXml
{
    public class CreditReportXmlService(
        IRequestManagerService requestManagerService,
        AsokiReportApiOptions options,
        RequestSecurity requestSecurity,
        LogWriter logWriter,
        IHelperRepository helperRepository, ICreditReportXmlParser creditReportXmlParser) : ICreditReportXmlService
    {
        private readonly IHelperRepository _helperRepository = helperRepository;
        private readonly ICreditReportXmlParser _creditReportXmlParser = creditReportXmlParser;
        private readonly IRequestManagerService _requestManagerService = requestManagerService;
        private readonly AsokiReportApiOptions _options = options;
        private readonly RequestSecurity _requestSecurity = requestSecurity;
        private readonly LogWriter _logWriter = logWriter;
        private const string CreditReport017FullLogFile = "CreditReport017Full.txt";
        public async Task CreditReportXml(LoanApplication loanApplications, CancellationToken cancellationToken)
        {
            try
            {
                if (loanApplications.QuantitySelected >= 5)
                {
                    await _helperRepository.KatmHelperXml(loanApplications.KeyLoanHistoryKb, "Запрос был отправлен больше 5 раз и не был правильно обработан!", IHelperRepository.TypeOperation.Error, cancellationToken);
                    return;
                }
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
                Console.WriteLine($"CI-017 XML Request. LoanKey:{loanApplications.KeyLoanHistoryKb} ClaimId:{loanApplications.PClaimId}\n{requestJson}");
                _logWriter.Log(
                    CreditReport017FullLogFile,
                    $"Type: CI-017 XML Request\nKeyLoanHistoryKb: {loanApplications.KeyLoanHistoryKb}\nClaimId: {loanApplications.PClaimId}\n{requestJson}");

                // Отправляем запрос
                var response = await _requestManagerService.SendPostRequest(
                    _options.HostAddress + _options.ReportUrl,
                    requestJson,
                    loanApplications.KeyLoanHistoryKb,
                    IRequestManagerRepository.IsXml.Xml,
                    cancellationToken);
                _logWriter.Log(
                    CreditReport017FullLogFile,
                    $"Type: CI-017 XML Response\nKeyLoanHistoryKb: {loanApplications.KeyLoanHistoryKb}\nClaimId: {loanApplications.PClaimId}\n{response}");
                if (string.IsNullOrWhiteSpace(response))
                    return;
                var baseResponse = JsonConvert.DeserializeObject<BaseResponse<CreditReportResponse>>(response);
                _logWriter.Log("CreditReportResponseXml.txt", $"KeyAbsLoan:ClaimId: {loanApplications.PClaimId} - KeyRequestHistoryKb:{loanApplications.KeyLoanHistoryKb} - {DateTime.Now}\n\n" + baseResponse?.ToJSON());
                // Проверяем запрос
                // Код ответа(05000 - успешно)
                if (baseResponse?.data?.result == CreditBureauResultCodes.SUCCESS_05000)
                {
                    // Сохраняем в базу данных Base64
                    try
                    {
                        await _helperRepository.KatmHelperXml(loanApplications.KeyLoanHistoryKb, baseResponse.data.reportBase64, IHelperRepository.TypeOperation.Base64, cancellationToken);
                        _logWriter.Log("TestParser.txt", "start");
                        try
                        {
                            await _creditReportXmlParser.ParseAndPersistAsync(loanApplications.KeyLoanHistoryKb, baseResponse.data.reportBase64, cancellationToken);
                            _logWriter.Log("TestParser.txt", "Успех");
                        }
                        catch (Exception ex)
                        {
                            _logWriter.Log("TestParser.txt", "catch 1--" + ex.Message);
                        }
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
                        await _helperRepository.KatmHelperXml(loanApplications.KeyLoanHistoryKb, baseResponse.data.token, IHelperRepository.TypeOperation.Token, cancellationToken);
                        return;
                    }
                }
                // Claim not found - Заявка не найдена
                else if (baseResponse?.data?.result == CreditBureauResultCodes.NO_TOKEN_FOUND)
                {
                    // Проверяем токен если токен не существует то записываем ошибку
                    if (string.IsNullOrEmpty(baseResponse.data.token))
                    {
                        await _helperRepository.KatmHelperXml(loanApplications.KeyLoanHistoryKb, "Заявка не найдена!", IHelperRepository.TypeOperation.Error, cancellationToken);
                        return;
                    }
                }
                else if (baseResponse?.data?.result == CreditBureauResultCodes.IDENTICAL_REQUEST)
                {
                    await _helperRepository.KatmHelperXml(loanApplications.KeyLoanHistoryKb, 5.ToJSON(), IHelperRepository.TypeOperation.AddNextAccess, cancellationToken);
                }
                else if (baseResponse?.data?.result == CreditBureauResultCodes.FREEZE_SERVICE_ACTIVE)
                {
                    await _helperRepository.KatmHelper(loanApplications.KeyLoanHistoryKb, "Субъект не дает согласия на получение кредитной истории, подключена услуга Freeze. Субъекту необходимо отключить услугу Freeze.", IHelperRepository.TypeOperation.Error, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                // Обработка ошибку если Попытка установить соединение была безуспешной, т.к.
                _logWriter.Log("CreditReportCatchXml.txt", $"KeyAbsLoan:ClaimId: {loanApplications.PClaimId} - KeyRequestHistoryKb:{loanApplications.KeyLoanHistoryKb} - {DateTime.Now}\n\n" + ex.Message);
                return;
            }
        }
        public async Task CreditReportStatusXml(LoanApplication loanApplications, CancellationToken cancellationToken)
        {
            try
            {
                if (loanApplications.QuantitySelected >= 5)
                {
                    await _helperRepository.KatmHelperXml(loanApplications.KeyLoanHistoryKb, "Запрос был отправлени больше 5 раз и не был правильно обработан!", IHelperRepository.TypeOperation.Error, cancellationToken);
                    return;
                }
                // подготавливаем запрос
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
                HttpContent content = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json");
                _logWriter.Log("CreditReportStatusRequestXml.txt", $"KeyAbsLoan:ClaimId: {loanApplications.PClaimId} - KeyRequestHistoryKb:{loanApplications.KeyLoanHistoryKb} - {DateTime.Now}\n\n" + requestJson);
                Console.WriteLine($"CI-017 XML Status Request. LoanKey:{loanApplications.KeyLoanHistoryKb} ClaimId:{loanApplications.PClaimId}\n{requestJson}");
                _logWriter.Log(
                    CreditReport017FullLogFile,
                    $"Type: CI-017 XML Status Request\nKeyLoanHistoryKb: {loanApplications.KeyLoanHistoryKb}\nClaimId: {loanApplications.PClaimId}\n{requestJson}");
                // Отправляем запрос
                var response = await _requestManagerService.SendPostRequest(
                    _options.HostAddress + _options.ReportStatusUrl,
                    requestJson,
                    loanApplications.KeyLoanHistoryKb,
                    IRequestManagerRepository.IsXml.Xml,
                    cancellationToken);
                _logWriter.Log(
                    CreditReport017FullLogFile,
                    $"Type: CI-017 XML Status Response\nKeyLoanHistoryKb: {loanApplications.KeyLoanHistoryKb}\nClaimId: {loanApplications.PClaimId}\n{response}");
                if (string.IsNullOrWhiteSpace(response))
                    return;
                var baseResponse = JsonConvert.DeserializeObject<BaseResponse<CreditReportStatusResponse>>(response);
                _logWriter.Log("CreditReportStatusResponseXml.txt", $"KeyAbsLoan:ClaimId: {loanApplications.PClaimId} - KeyRequestHistoryKb:{loanApplications.KeyLoanHistoryKb} - {DateTime.Now}\n\n" + baseResponse?.ToJSON());

                if (baseResponse.data.result == CreditBureauResultCodes.SUCCESS_05000)
                {
                    // Сохраняем в базу данных Base64
                    await _helperRepository.KatmHelperXml(loanApplications.KeyLoanHistoryKb, baseResponse.data.reportBase64, IHelperRepository.TypeOperation.Base64, cancellationToken);
                    // Парсинг данных и хранение их в БД
                    _logWriter.Log("TestParser.txt", "start");
                    try
                    {
                        await _creditReportXmlParser.ParseAndPersistAsync(loanApplications.KeyLoanHistoryKb, baseResponse.data.reportBase64, cancellationToken);
                        _logWriter.Log("TestParser.txt", "Успех");
                    }
                    catch (Exception ex)
                    {
                        _logWriter.Log("TestParser.txt", "catch 1--" + ex.Message);
                    }
                }
                else if (baseResponse.data.result == CreditBureauResultCodes.IDENTICAL_REQUEST)
                {
                    await _helperRepository.KatmHelperXml(loanApplications.KeyLoanHistoryKb, 5.ToJSON(), IHelperRepository.TypeOperation.AddNextAccess, cancellationToken);
                }
                else if (baseResponse.data.result == CreditBureauResultCodes.WAIT_AND_TRY_AGAIN)
                {
                    await _helperRepository.KatmHelperXml(loanApplications.KeyLoanHistoryKb, 1.ToJSON(), IHelperRepository.TypeOperation.AddNextAccess, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logWriter.Log("CreditReportStatusResponseXml.txt", $"KeyAbsLoan:ClaimId: {loanApplications.PClaimId} - KeyRequestHistoryKb:{loanApplications.KeyLoanHistoryKb} - {DateTime.Now}\n\n" + ex.Message);
                return;
            }
        }
    }
}
