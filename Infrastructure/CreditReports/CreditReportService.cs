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
using Infrastructure.Services.Notifications;
using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace Infrastructure.CreditReports
{
    public class CreditReportService(
        IRequestManagerService requestManagerService,
        CreditBureauReportApiOptions options,
        RequestSecurity requestSecurity,
        LogWriter logWriter,
        IHelperRepository helperRepository,
        IRequestManagerRepository requestManagerRepository,
        ICreditBureauReportRepository creditBureauReportRepository,
        ITelegramNotificationService telegramNotificationService) : ICreditReportService
    {
        private readonly IHelperRepository _helperRepository = helperRepository;
        private readonly IRequestManagerService _requestManagerService = requestManagerService;
        private readonly CreditBureauReportApiOptions _options = options;
        private readonly RequestSecurity _requestSecurity = requestSecurity;
        private readonly LogWriter _logWriter = logWriter;
        private readonly IRequestManagerRepository _requestManagerRepository = requestManagerRepository;
        private readonly ICreditBureauReportRepository _creditBureauReportRepository = creditBureauReportRepository;
        private readonly ITelegramNotificationService _telegramNotificationService = telegramNotificationService;
        private const string CreditReport017FullLogFile = "CreditReport017Full.txt";
        private const int MaxCi017Attempts = 3;
        private readonly ConcurrentDictionary<(int LoanKey, string? ReportId), byte> _notifiedMaxAttempts = new();

        public async Task CreditReport(LoanApplication loanApplications, CancellationToken cancellationToken)
        {
            // KeyCreditBureauKb = Loan_History_KB.[key]; Katm_Methods_Request uses Loan.[key] via App.
            var historyKey = int.Parse(loanApplications.KeyCreditBureauKb);
            var loanKeyNullable = await _creditBureauReportRepository.GetLoanKeyByHistoryKbAsync(historyKey, cancellationToken);
            if (loanKeyNullable is null)
            {
                await _creditBureauReportRepository.UpdateRequestHistoryStatusAsync(historyKey, "09", cancellationToken, loanApplications.PReportId,
                    $"Для отчёта CI-017 {loanApplications.PReportId} не найдена запись займа.");
                return;
            }

            var loanKey = loanKeyNullable.Value;

            var isIndividual = loanApplications.ApplicationsSubjectType == "0";
            var prerequisiteStatus = isIndividual
                ? await _creditBureauReportRepository.GetCreditBureau001StatusAsync(loanKey, cancellationToken)
                : await _creditBureauReportRepository.GetCreditBureau002StatusAsync(loanKey, cancellationToken);

            if (prerequisiteStatus != 1)
            {
                await _creditBureauReportRepository.UpdateRequestHistoryStatusAsync(historyKey, "09", cancellationToken, loanApplications.PReportId,
                    $"Для отчёта CI-017 {loanApplications.PReportId} не выполнено предварительное условие CI-00{(isIndividual ? 1 : 2)}.");
                return;
            }

            var ci017State = await _creditBureauReportRepository.GetCi017StateAsync(loanKey, loanApplications.PReportId, cancellationToken);
            if (ci017State.AttemptCount >= MaxCi017Attempts)
            {
                if (_notifiedMaxAttempts.TryAdd((loanKey, loanApplications.PReportId), 0))
                {
                    await NotifyErrorAsync("CI-017 max attempts", loanApplications, $"Max attempts ({MaxCi017Attempts}) reached", cancellationToken);
                }
                await _creditBureauReportRepository.UpsertCiStatusAsync(loanKey, 17, 2, $"Max attempts ({MaxCi017Attempts}) reached", null, cancellationToken);
                await _creditBureauReportRepository.UpdateRequestHistoryStatusAsync(historyKey, "09", cancellationToken, loanApplications.PReportId,
                    $"CI-017 отчёт {loanApplications.PReportId}: достигнут лимит попыток ({MaxCi017Attempts}).");
                return;
            }

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
                Console.WriteLine($"CI-017 Request. LoanKey:{loanApplications.KeyCreditBureauKb} ClaimId:{loanApplications.PClaimId}\n{requestJson.RedactSecurity()}");
                _logWriter.Log(
                    CreditReport017FullLogFile,
                    $"Type: CI-017 Request\nKeyLoanHistoryKb: {loanApplications.KeyCreditBureauKb}\nClaimId: {loanApplications.PClaimId}\n{requestJson.RedactSecurity()}");

                // Отправляем запрос
                var dateRequest = DateTime.Now;
                var response = await _requestManagerService.SendPostRequest(
                    _options.HostAddress + _options.ReportUrl,
                    requestJson,
                    loanApplications.KeyCreditBureauKb,
                    IRequestManagerRepository.IsXml.NotXml,
                    cancellationToken,
                    loanApplications.PReportId);
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

                await _creditBureauReportRepository.IncrementCi017AttemptAsync(loanKey, loanApplications.PReportId, loanApplications.Status, cancellationToken);

                if (string.IsNullOrWhiteSpace(response))
                {
                    return;
                }
                var baseResponse = JsonConvert.DeserializeObject<BaseResponse<CreditReportResponse>>(response);
                _logWriter.Log("CreditReportResponse.txt", $"KeyAbsLoan:ClaimId: {loanApplications.PClaimId} - KeyRequestHistoryKb:{loanApplications.KeyCreditBureauKb} - {DateTime.Now}\n\n" + baseResponse?.ToJSON());
                // Проверяем запрос
                // Код ответа(05000 - успешно)
                if (baseResponse?.data?.result == CreditBureauResultCodes.SUCCESS_05000)
                {
                    // Сохраняем в базу данных Base64
                    try
                    {
                        await _helperRepository.KatmHelper(loanApplications.KeyCreditBureauKb, baseResponse.data.reportBase64, IHelperRepository.TypeOperation.Base64, cancellationToken, loanApplications.PReportId);
                        await _creditBureauReportRepository.UpsertCiStatusAsync(loanKey, 17, 1, "Success", null, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logWriter.Log("SaveReportBase64.txt", baseResponse.data.reportBase64 + "\n\n" + ex.Message);
                        try
                        {
                            await _helperRepository.KatmHelper(loanApplications.KeyCreditBureauKb,
                                $"Не удалось сохранить отчёт CI-017 {loanApplications.PReportId}. Подробности записаны в журнал сервиса.",
                                IHelperRepository.TypeOperation.Comment, cancellationToken, loanApplications.PReportId);
                        }
                        catch { }
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
                        await _helperRepository.KatmHelper(loanApplications.KeyCreditBureauKb, baseResponse.data.token, IHelperRepository.TypeOperation.Token, cancellationToken, loanApplications.PReportId);
                        await _creditBureauReportRepository.UpsertCiStatusAsync(loanKey, 17, 0, "Waiting", baseResponse.data.token, cancellationToken);
                        // Status check must wait ≥ CheckReportStatusInterval (60s) after this attempt —
                        // worker will call CreditReportStatus once the interval elapses.
                        loanApplications.PToken = baseResponse.data.token;
                        return;
                    }
                    await _helperRepository.KatmHelper(loanApplications.KeyCreditBureauKb,
                        $"Отчёт CI-017 {loanApplications.PReportId}: ожидается ответ КАТМ, токен пока не получен.",
                        IHelperRepository.TypeOperation.Comment, cancellationToken, loanApplications.PReportId);
                }
                // Claim not found - Заявка не найдена
                else if (baseResponse?.data?.result == CreditBureauResultCodes.NO_TOKEN_FOUND)
                {
                    if (string.IsNullOrEmpty(baseResponse.data.token))
                    {
                        await _helperRepository.KatmHelper(loanApplications.KeyCreditBureauKb, "Заявка не найдена!", IHelperRepository.TypeOperation.Error, cancellationToken, loanApplications.PReportId);
                        await _creditBureauReportRepository.UpsertCiStatusAsync(loanKey, 17, 2, "Claim not found", null, cancellationToken);
                        return;
                    }
                }
                else if (baseResponse?.data?.result == CreditBureauResultCodes.IDENTICAL_REQUEST)
                {
                    await _helperRepository.KatmHelper(loanApplications.KeyCreditBureauKb, 5.ToJSON(), IHelperRepository.TypeOperation.AddNextAccess, cancellationToken, loanApplications.PReportId);
                    await _creditBureauReportRepository.UpsertCiStatusAsync(loanKey, 17, 2, "Identical request", null, cancellationToken);
                }
                else if (baseResponse?.data?.result == CreditBureauResultCodes.FREEZE_SERVICE_ACTIVE)
                {
                    await _helperRepository.KatmHelper(loanApplications.KeyCreditBureauKb, "Субъект не дает согласия на получение кредитной истории, подключена услуга Freeze. Субъекту необходимо отключить услугу Freeze.", IHelperRepository.TypeOperation.Error, cancellationToken, loanApplications.PReportId);
                    await _creditBureauReportRepository.UpsertCiStatusAsync(loanKey, 17, 2, "Freeze service active", null, cancellationToken);
                }
                else if (baseResponse?.data?.result is not null)
                {
                    await _helperRepository.KatmHelper(loanApplications.KeyCreditBureauKb,
                        $"КАТМ вернул результат {baseResponse.data.result} для отчёта CI-017 {loanApplications.PReportId}.",
                        IHelperRepository.TypeOperation.Comment, cancellationToken, loanApplications.PReportId);
                }
            }
            catch (Exception ex)
            {
                _logWriter.Log("CreditReportCatch.txt", $"KeyAbsLoan:ClaimId: {loanApplications.PClaimId} - KeyRequestHistoryKb:{loanApplications.KeyCreditBureauKb} - {DateTime.Now}\n\n" + ex.Message);
                if (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        await _helperRepository.KatmHelper(loanApplications.KeyCreditBureauKb,
                            $"Ошибка при обработке отчёта CI-017 {loanApplications.PReportId}. Подробности записаны в журнал сервиса.",
                            IHelperRepository.TypeOperation.Comment, cancellationToken, loanApplications.PReportId);
                    }
                    catch { }
                }
                return;
            }
        }
        public async Task CreditReportStatus(LoanApplication loanApplications, CancellationToken cancellationToken, bool skipIntervalCheck = false)
        {
            var historyKey = int.Parse(loanApplications.KeyCreditBureauKb);
            var loanKeyNullable = await _creditBureauReportRepository.GetLoanKeyByHistoryKbAsync(historyKey, cancellationToken);
            if (loanKeyNullable is null)
            {
                await _creditBureauReportRepository.UpdateRequestHistoryStatusAsync(historyKey, "09", cancellationToken, loanApplications.PReportId,
                    $"Для проверки статуса отчёта CI-017 {loanApplications.PReportId} не найдена запись займа.");
                return;
            }

            var loanKey = loanKeyNullable.Value;

            var ci017State = await _creditBureauReportRepository.GetCi017StateAsync(loanKey, loanApplications.PReportId, cancellationToken);
            if (ci017State.AttemptCount >= MaxCi017Attempts)
            {
                if (_notifiedMaxAttempts.TryAdd((loanKey, loanApplications.PReportId), 0))
                {
                    await NotifyErrorAsync("CI-017 max attempts", loanApplications, $"Max attempts ({MaxCi017Attempts}) reached", cancellationToken);
                }
                await _creditBureauReportRepository.UpsertCiStatusAsync(loanKey, 17, 2, $"Max attempts ({MaxCi017Attempts}) reached", null, cancellationToken);
                await _creditBureauReportRepository.UpdateRequestHistoryStatusAsync(historyKey, "09", cancellationToken, loanApplications.PReportId,
                    $"CI-017 отчёт {loanApplications.PReportId}: достигнут лимит попыток проверки статуса ({MaxCi017Attempts}).");
                return;
            }

            // LastCi017AttemptAt хранится в БД как локальное время Ташкента (UTC+5)
            if (!skipIntervalCheck && ci017State.LastAttemptAt is not null &&
                DateTime.UtcNow.AddHours(5) - ci017State.LastAttemptAt.Value < TimeSpan.FromMilliseconds(_options.CheckReportStatusInterval))
            {
                return;
            }

            // Без токена нечего проверять на бюро - значит запрос CI-017 (/credit/report) ещё не выполнялся
            if (string.IsNullOrEmpty(loanApplications.PToken))
            {
                await _helperRepository.KatmHelper(loanApplications.KeyCreditBureauKb,
                    $"Отчёт CI-017 {loanApplications.PReportId}: ожидается получение токена.",
                    IHelperRepository.TypeOperation.Comment, cancellationToken, loanApplications.PReportId);
                return;
            }

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
                _logWriter.Log("CreditReportStatusRequest.txt", $"KeyAbsLoan:ClaimId: {loanApplications.PClaimId} - KeyRequestHistoryKb:{loanApplications.KeyCreditBureauKb} - {DateTime.Now}\n\n" + requestJson.RedactSecurity());
                Console.WriteLine($"CI-017 Status Request. LoanKey:{loanApplications.KeyCreditBureauKb} ClaimId:{loanApplications.PClaimId}\n{requestJson.RedactSecurity()}");
                _logWriter.Log(
                    CreditReport017FullLogFile,
                    $"Type: CI-017 Status Request\nKeyLoanHistoryKb: {loanApplications.KeyCreditBureauKb}\nClaimId: {loanApplications.PClaimId}\n{requestJson.RedactSecurity()}");

                var dateRequest = DateTime.Now;
                var response = await _requestManagerService.SendPostRequest(
                    _options.HostAddress + _options.ReportStatusUrl,
                    requestJson,
                    loanApplications.KeyCreditBureauKb,
                    IRequestManagerRepository.IsXml.NotXml,
                    cancellationToken,
                    loanApplications.PReportId);
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

                await _creditBureauReportRepository.IncrementCi017AttemptAsync(loanKey, loanApplications.PReportId, loanApplications.Status, cancellationToken);

                if (string.IsNullOrWhiteSpace(response))
                {
                    return;
                }

                var baseResponse = JsonConvert.DeserializeObject<BaseResponse<CreditReportStatusResponse>>(response);
                _logWriter.Log("CreditReportStatusResponse.txt", $"KeyAbsLoan:ClaimId: {loanApplications.PClaimId} - KeyRequestHistoryKb:{loanApplications.KeyCreditBureauKb} - {DateTime.Now}\n\n" + baseResponse?.ToJSON());

                if (baseResponse.data.result == CreditBureauResultCodes.SUCCESS_05000)
                {
                    await _helperRepository.KatmHelper(loanApplications.KeyCreditBureauKb, baseResponse.data.reportBase64, IHelperRepository.TypeOperation.Base64, cancellationToken, loanApplications.PReportId);
                    await _creditBureauReportRepository.UpsertCiStatusAsync(loanKey, 17, 1, "Success", null, cancellationToken);
                    return;
                }
                else if (baseResponse.data.result == CreditBureauResultCodes.IDENTICAL_REQUEST)
                {
                    await _helperRepository.KatmHelper(loanApplications.KeyCreditBureauKb, 5.ToJSON(), IHelperRepository.TypeOperation.AddNextAccess, cancellationToken, loanApplications.PReportId);
                    await _creditBureauReportRepository.UpsertCiStatusAsync(loanKey, 17, 2, "Identical request", null, cancellationToken);
                    return;
                }
                else if (baseResponse.data.result == CreditBureauResultCodes.WAIT_AND_TRY_AGAIN)
                {
                    await _helperRepository.KatmHelper(loanApplications.KeyCreditBureauKb, 1.ToJSON(), IHelperRepository.TypeOperation.AddNextAccess, cancellationToken, loanApplications.PReportId);
                    await _creditBureauReportRepository.UpsertCiStatusAsync(loanKey, 17, 0, "Waiting", loanApplications.PToken, cancellationToken);
                    return;
                }
                else
                {
                    await _helperRepository.KatmHelper(loanApplications.KeyCreditBureauKb,
                        $"Проверка статуса отчёта CI-017 {loanApplications.PReportId} вернула результат {baseResponse.data.result}.",
                        IHelperRepository.TypeOperation.Comment, cancellationToken, loanApplications.PReportId);
                    return;
                }
            }
            catch (Exception ex)
            {
                _logWriter.Log("CreditReportStatusResponse.txt", $"KeyAbsLoan:ClaimId: {loanApplications.PClaimId} - KeyRequestHistoryKb:{loanApplications.KeyCreditBureauKb} - {DateTime.Now}\n\n" + ex.Message);
                if (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        await _helperRepository.KatmHelper(loanApplications.KeyCreditBureauKb,
                            $"Ошибка при проверке статуса отчёта CI-017 {loanApplications.PReportId}. Подробности записаны в журнал сервиса.",
                            IHelperRepository.TypeOperation.Comment, cancellationToken, loanApplications.PReportId);
                    }
                    catch { }
                }
                return;
            }
        }

        private async Task NotifyErrorAsync(string source, LoanApplication loan, string details, CancellationToken cancellationToken)
        {
            var (app, customerId) = await _creditBureauReportRepository.GetLoanAppAndCustomerIdAsync(
                loan.KeyCreditBureauKb, cancellationToken);

            await _telegramNotificationService.NotifyErrorAsync(
                source,
                TelegramErrorFormatter.Format(
                    category: "Лимит попыток CI-017",
                    reason: details,
                    action: "Проверьте статус заявки в КАТМ и историю CI-017; при необходимости обработайте вручную.",
                    context: source,
                    loanKey: loan.KeyCreditBureauKb,
                    claimId: loan.PClaimId),
                app,
                customerId,
                cancellationToken);
        }
    }
}
