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
using Infrastructure.Services.HttpClients;
using Newtonsoft.Json;

namespace Infrastructure.CreditReports
{
    public class CreditReportService(
        IRequestManagerService requestManagerService,
        CreditBureauReportApiOptions options,
        RequestSecurity requestSecurity,
        LogWriter logWriter,
        IHelperRepository helperRepository,
        IRequestManagerRepository requestManagerRepository,
        ICreditBureauReportRepository creditBureauReportRepository) : ICreditReportService
    {
        private readonly IHelperRepository _helperRepository = helperRepository;
        private readonly IRequestManagerService _requestManagerService = requestManagerService;
        private readonly CreditBureauReportApiOptions _options = options;
        private readonly RequestSecurity _requestSecurity = requestSecurity;
        private readonly LogWriter _logWriter = logWriter;
        private readonly IRequestManagerRepository _requestManagerRepository = requestManagerRepository;
        private readonly ICreditBureauReportRepository _creditBureauReportRepository = creditBureauReportRepository;
        private const string CreditReport017FullLogFile = "CreditReport017Full.txt";
        public async Task CreditReport(LoanApplication loanApplications, CancellationToken cancellationToken)
        {
            try
            {
                if (loanApplications.QuantitySelected >= 5)
                {
                    await _helperRepository.KatmHelper(loanApplications.KeyCreditBureauKb, "Запрос был отправлени больше 5 раз и не был правильно обработан!", IHelperRepository.TypeOperation.Error, cancellationToken);
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
                    PReportReason = 1
                };
                var request = new BaseRequest<CreditReportRequest>() { Data = creditReportRequest, Security = _requestSecurity };
                var requestJson = request.ToJSON();
                Console.WriteLine($"CI-017 Request. LoanKey:{loanApplications.KeyCreditBureauKb} ClaimId:{loanApplications.PClaimId}\n{requestJson}");
                _logWriter.Log(
                    CreditReport017FullLogFile,
                    $"Type: CI-017 Request\nKeyLoanHistoryKb: {loanApplications.KeyCreditBureauKb}\nClaimId: {loanApplications.PClaimId}\n{requestJson}");

                // Отправляем запрос
                var dateRequest = DateTime.Now;
                var response = await _requestManagerService.SendPostRequest(
                    _options.HostAddress + _options.ReportUrl,
                    requestJson,
                    loanApplications.KeyCreditBureauKb,
                    IRequestManagerRepository.IsXml.NotXml,
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
                    $"Type: CI-017 Response\nKeyLoanHistoryKb: {loanApplications.KeyCreditBureauKb}\nClaimId: {loanApplications.PClaimId}\n{response}");
                if (string.IsNullOrWhiteSpace(response))
                    return;
                var baseResponse = JsonConvert.DeserializeObject<BaseResponse<CreditReportResponse>>(response);
                _logWriter.Log("CreditReportResponse.txt", $"KeyAbsLoan:ClaimId: {loanApplications.PClaimId} - KeyRequestHistoryKb:{loanApplications.KeyCreditBureauKb} - {DateTime.Now}\n\n" + baseResponse?.ToJSON());
                // Проверяем запрос
                // Код ответа(05000 - успешно)
                if (baseResponse?.data?.result == CreditBureauResultCodes.SUCCESS_05000)
                {
                    // Сохраняем в базу данных Base64
                    try
                    {
                        await _helperRepository.KatmHelper(loanApplications.KeyCreditBureauKb, baseResponse.data.reportBase64, IHelperRepository.TypeOperation.Base64, cancellationToken);
                        await _creditBureauReportRepository.UpsertCiStatusAsync(int.Parse(loanApplications.KeyCreditBureauKb), 17, 1, "Success", null, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logWriter.Log("SaveReportBase64.txt", baseResponse.data.reportBase64 + "\n\n" + ex.Message);
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
                        await _helperRepository.KatmHelper(loanApplications.KeyCreditBureauKb, baseResponse.data.token, IHelperRepository.TypeOperation.Token, cancellationToken);
                        await _creditBureauReportRepository.UpsertCiStatusAsync(int.Parse(loanApplications.KeyCreditBureauKb), 17, 0, "Waiting", baseResponse.data.token, cancellationToken);
                        return;
                    }
                }
                // Claim not found - Заявка не найдена
                else if (baseResponse?.data?.result == CreditBureauResultCodes.NO_TOKEN_FOUND)
                {
                    // Проверяем токен если токен не существует то записываем ошибку
                    if (string.IsNullOrEmpty(baseResponse.data.token))
                    {
                        await _helperRepository.KatmHelper(loanApplications.KeyCreditBureauKb, "Заявка не найдена!", IHelperRepository.TypeOperation.Error, cancellationToken);
                        await _creditBureauReportRepository.UpsertCiStatusAsync(int.Parse(loanApplications.KeyCreditBureauKb), 17, 2, "Claim not found", null, cancellationToken);
                        return;
                    }
                }
                else if (baseResponse?.data?.result == CreditBureauResultCodes.IDENTICAL_REQUEST)
                {
                    await _helperRepository.KatmHelper(loanApplications.KeyCreditBureauKb, 5.ToJSON(), IHelperRepository.TypeOperation.AddNextAccess, cancellationToken);
                    await _creditBureauReportRepository.UpsertCiStatusAsync(int.Parse(loanApplications.KeyCreditBureauKb), 17, 2, "Identical request", null, cancellationToken);
                }
                else if (baseResponse?.data?.result == CreditBureauResultCodes.FREEZE_SERVICE_ACTIVE)
                {
                    await _helperRepository.KatmHelper(loanApplications.KeyCreditBureauKb, "Субъект не дает согласия на получение кредитной истории, подключена услуга Freeze. Субъекту необходимо отключить услугу Freeze.", IHelperRepository.TypeOperation.Error, cancellationToken);
                    await _creditBureauReportRepository.UpsertCiStatusAsync(int.Parse(loanApplications.KeyCreditBureauKb), 17, 2, "Freeze service active", null, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                // Обработка ошибку если Попытка установить соединение была безуспешной, т.к.
                _logWriter.Log("CreditReportCatch.txt", $"KeyAbsLoan:ClaimId: {loanApplications.PClaimId} - KeyRequestHistoryKb:{loanApplications.KeyCreditBureauKb} - {DateTime.Now}\n\n" + ex.Message);
                return;
            }
        }
        public async Task CreditReportStatus(LoanApplication loanApplications, CancellationToken cancellationToken)
        {
            var maxAttempts = 5;
            var attempts = loanApplications.QuantitySelected ?? 0;

            while (attempts < maxAttempts && !cancellationToken.IsCancellationRequested)
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
                    _logWriter.Log("CreditReportStatusRequest.txt", $"KeyAbsLoan:ClaimId: {loanApplications.PClaimId} - KeyRequestHistoryKb:{loanApplications.KeyCreditBureauKb} - {DateTime.Now}\n\n" + requestJson);
                    Console.WriteLine($"CI-017 Status Request. LoanKey:{loanApplications.KeyCreditBureauKb} ClaimId:{loanApplications.PClaimId}\n{requestJson}");
                    _logWriter.Log(
                        CreditReport017FullLogFile,
                        $"Type: CI-017 Status Request\nKeyLoanHistoryKb: {loanApplications.KeyCreditBureauKb}\nClaimId: {loanApplications.PClaimId}\n{requestJson}");

                    var dateRequest = DateTime.Now;
                    var response = await _requestManagerService.SendPostRequest(
                        _options.HostAddress + _options.ReportStatusUrl,
                        requestJson,
                        loanApplications.KeyCreditBureauKb,
                        IRequestManagerRepository.IsXml.NotXml,
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
                        $"Type: CI-017 Status Response\nKeyLoanHistoryKb: {loanApplications.KeyCreditBureauKb}\nClaimId: {loanApplications.PClaimId}\n{response}");
                    if (string.IsNullOrWhiteSpace(response))
                        return;

                    var baseResponse = JsonConvert.DeserializeObject<BaseResponse<CreditReportStatusResponse>>(response);
                    _logWriter.Log("CreditReportStatusResponse.txt", $"KeyAbsLoan:ClaimId: {loanApplications.PClaimId} - KeyRequestHistoryKb:{loanApplications.KeyCreditBureauKb} - {DateTime.Now}\n\n" + baseResponse?.ToJSON());

                    if (baseResponse.data.result == CreditBureauResultCodes.SUCCESS_05000)
                    {
                        await _helperRepository.KatmHelper(loanApplications.KeyCreditBureauKb, baseResponse.data.reportBase64, IHelperRepository.TypeOperation.Base64, cancellationToken);
                        await _creditBureauReportRepository.UpsertCiStatusAsync(int.Parse(loanApplications.KeyCreditBureauKb), 17, 1, "Success", null, cancellationToken);
                        return;
                    }
                    else if (baseResponse.data.result == CreditBureauResultCodes.IDENTICAL_REQUEST)
                    {
                        await _helperRepository.KatmHelper(loanApplications.KeyCreditBureauKb, 5.ToJSON(), IHelperRepository.TypeOperation.AddNextAccess, cancellationToken);
                        await _creditBureauReportRepository.UpsertCiStatusAsync(int.Parse(loanApplications.KeyCreditBureauKb), 17, 2, "Identical request", null, cancellationToken);
                        return;
                    }
                    else if (baseResponse.data.result == CreditBureauResultCodes.WAIT_AND_TRY_AGAIN)
                    {
                        attempts++;
                        await _helperRepository.KatmHelper(loanApplications.KeyCreditBureauKb, 1.ToJSON(), IHelperRepository.TypeOperation.AddNextAccess, cancellationToken);
                        await _creditBureauReportRepository.UpsertCiStatusAsync(int.Parse(loanApplications.KeyCreditBureauKb), 17, 0, "Waiting", loanApplications.PToken, cancellationToken);

                        if (attempts >= maxAttempts)
                        {
                            await _helperRepository.KatmHelper(loanApplications.KeyCreditBureauKb, "Запрос был отправлени больше 5 раз и не был правильно обработан!", IHelperRepository.TypeOperation.Error, cancellationToken);
                            return;
                        }

                        await Task.Delay(_options.CheckReportStatusInterval, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logWriter.Log("CreditReportStatusResponse.txt", $"KeyAbsLoan:ClaimId: {loanApplications.PClaimId} - KeyRequestHistoryKb:{loanApplications.KeyCreditBureauKb} - {DateTime.Now}\n\n" + ex.Message);
                    return;
                }
            }
        }
    }
}
