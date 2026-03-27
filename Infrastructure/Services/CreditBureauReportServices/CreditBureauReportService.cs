using Application.Repositories.CreditBureauReportRepositories;
using Application.Repositories.Helpers;
using CreditBureau.Contracts.AsokiLoanApplications.CreditRegistration.CreditAgreementsAndLeasing.Requests;
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
using System.Diagnostics;

namespace Infrastructure.Services.CreditBureauReportServices;

public class CreditBureauReportService(ICreditBureauReportRepository creditBureauReportRepository,
    IRequestManagerService requestManagerService,
                                           BankHeader bankHeader,
                                        LogWriter logWriter,
                                        AsokiApplicationApiOptions asokiApplicationApiOptions,
                                        IHelperRepository helperRepository,
                                        ILogger<CreditBureauReportService> logger,
                                        RequestSecurity requestSecurity,
                                        AsokiReportApiOptions asokiReportApiOptions) : ICreditBureauReportService
{
    private readonly ICreditBureauReportRepository _creditBureauReportRepository = creditBureauReportRepository;
    private readonly IHelperRepository _helperRepository = helperRepository;
    private readonly IRequestManagerService _requestManagerService = requestManagerService;
    private readonly BankHeader _bankHeader = bankHeader;
    private readonly LogWriter _logWriter = logWriter;
    private readonly AsokiApplicationApiOptions _asokiApplicationApiOptions = asokiApplicationApiOptions;
    private readonly ILogger<CreditBureauReportService> _logger = logger;
    private readonly RequestSecurity _requestSecurity = requestSecurity;
    private readonly AsokiReportApiOptions _asokiReportApiOptions = asokiReportApiOptions;
    public async Task CreditBureauReportProcessing(CancellationToken cancellationToken)
    {
        var processingStopwatch = Stopwatch.StartNew();
        _logger.LogInformation("CreditBureauReportProcessing started.");

        var ci001Processed = 0;
        var ci001Success = 0;
        var ci001Error = 0;

        var ci002Processed = 0;
        var ci002Success = 0;
        var ci002Error = 0;

        var ci003Processed = 0;
        var ci003Success = 0;
        var ci003Error = 0;

        var ci004Processed = 0;
        var ci004Success = 0;
        var ci004Error = 0;

        var ci005Processed = 0;
        var ci005Success = 0;
        var ci005Error = 0;

        var ci011Processed = 0;
        var ci011Success = 0;
        var ci011Error = 0;

        var ci012Processed = 0;
        var ci012Success = 0;
        var ci012Error = 0;

        var ci013Processed = 0;
        var ci013Success = 0;
        var ci013Error = 0;

        var ci014Processed = 0;
        var ci014Success = 0;
        var ci014Error = 0;

        var ci015Processed = 0;
        var ci015Success = 0;
        var ci015Error = 0;

        var ci016Processed = 0;
        var ci016Success = 0;
        var ci016Error = 0;

        var ci018Processed = 0;
        var ci018Success = 0;
        var ci018Error = 0;

        var ci020Processed = 0;
        var ci020Success = 0;
        var ci020Error = 0;

        var ci021Processed = 0;
        var ci021Success = 0;
        var ci021Error = 0;

        var ci022Processed = 0;
        var ci022Success = 0;
        var ci022Error = 0;

        var ci023Processed = 0;
        var ci023Success = 0;
        var ci023Error = 0;

        var ci017Processed = 0;
        var ci017Success = 0;
        var ci017Error = 0;
        var ci017Pending = 0;

        // 1. Отправка заявки физ.лица в кредитное бюро запрос (001)
        var creditBureauReportQueueItems = await _creditBureauReportRepository.GetCreditRegistrationIndividualRequestsAsync(cancellationToken);
        _logger.LogInformation("CI-001 queue loaded. Count={Count}", creditBureauReportQueueItems.Count);

        foreach (var creditBureauReportQueueItem in creditBureauReportQueueItems)
        {
            ci001Processed++;
            var individualRequest = creditBureauReportQueueItem.Request;
            if (individualRequest is null)
            {
                ci001Error++;
                _logger.LogWarning("CI-001 skipped due to null request. LoanKey={LoanKey}", creditBureauReportQueueItem.LoanKey);
                continue;
            }

            var baseRequestForCredit = new BaseRequestForCreditApplications<CreditRegistrationIndividualRequest>()
            {
                Header = _bankHeader,
                Request = individualRequest,
                Security = _requestSecurity
            };

            var response = await _requestManagerService.SendPostRequest(
                     _asokiApplicationApiOptions.HostAddress + _asokiApplicationApiOptions.IndividualPersonApplicationUrl,
                     baseRequestForCredit.ToJSON(),
                     creditBureauReportQueueItem.LoanKey,
                     cancellationToken);

            if (string.IsNullOrWhiteSpace(response))
            {
                ci001Error++;
                const string emptyResponseMessage = "CI-001 returned empty response";
                await _creditBureauReportRepository.UpsertCiStatusAsync(
                    creditBureauReportQueueItem.LoanKey,
                    1,
                    2,
                    emptyResponseMessage,
                    null,
                    cancellationToken);
                continue;
            }

            var baseResponse = JsonConvert.DeserializeObject<CreditRegistrationSubjectResponse>(response);
            var isSuccess = baseResponse?.Result?.Code is CreditBureauResultCodes.SUCCESS_00000 or CreditBureauResultCodes.SUCCESS_05000;
            var ciStatus = (byte)(isSuccess ? 1 : 2);
            var token = baseResponse?.Response?.KatmSir;
            var message = baseResponse?.Result?.Message ?? (isSuccess ? "Success" : "Unknown error");

            // Успешно
            if (isSuccess)
            {
                ci001Success++;
                _logger.LogInformation(
                    "LoanKey:{LoanKey} CI-001 success. KatmSir:{KatmSir}",
                    creditBureauReportQueueItem.LoanKey,
                    token);
                _logWriter.Log(
                    "CreditRegistrationIndividual.txt",
                    $"LoanKey:{creditBureauReportQueueItem.LoanKey} CI-001 success. KatmSir:\t{token}");
            }
            else
            {
                ci001Error++;
                _logger.LogError(
                    "LoanKey:{LoanKey} CI-001 error. Response:{Response}",
                    creditBureauReportQueueItem.LoanKey,
                    response);
                _logWriter.Log(
                    "CreditRegistrationIndividual.txt",
                    $"LoanKey:{creditBureauReportQueueItem.LoanKey} CI-001 error. Response:{response}");
            }

            await _creditBureauReportRepository.UpsertCiStatusAsync(
                creditBureauReportQueueItem.LoanKey,
                1,
                ciStatus,
                message,
                token,
                cancellationToken);
        }

        _logger.LogInformation(
            "CI-001 completed. Processed={Processed}, Success={Success}, Error={Error}",
            ci001Processed,
            ci001Success,
            ci001Error);

        // 2. Отправка заявки юр.лица в кредитное бюро запрос (002)
        var entityRequests = await _creditBureauReportRepository.GetCreditRegistrationEntityRequestsAsync(cancellationToken);
        _logger.LogInformation("CI-002 queue loaded. Count={Count}", entityRequests.Count);
        foreach (var entityRequest in entityRequests)
        {
            ci002Processed++;
            var request = entityRequest.Request;
            if (request is null)
            {
                ci002Error++;
                const string nullRequestMessage = "CI-002 request is null";
                _logger.LogWarning("CI-002 skipped due to null request. LoanKey={LoanKey}", entityRequest.LoanKey);
                await _creditBureauReportRepository.UpsertCiStatusAsync(
                    entityRequest.LoanKey,
                    2,
                    2,
                    nullRequestMessage,
                    null,
                    cancellationToken);
                continue;
            }

            var baseRequestForCredit = new BaseRequestForCreditApplications<CreditRegistrationEntityRequest>()
            {
                Header = _bankHeader,
                Request = request,
                Security = _requestSecurity
            };

            var response = await _requestManagerService.SendPostRequest(
                _asokiApplicationApiOptions.HostAddress + _asokiApplicationApiOptions.LegalEntityApplicationUrl,
                baseRequestForCredit.ToJSON(),
                entityRequest.LoanKey,
                cancellationToken);

            if (string.IsNullOrWhiteSpace(response))
            {
                ci002Error++;
                const string emptyResponseMessage = "CI-002 returned empty response";
                await _creditBureauReportRepository.UpsertCiStatusAsync(
                    entityRequest.LoanKey,
                    2,
                    2,
                    emptyResponseMessage,
                    null,
                    cancellationToken);
                continue;
            }

            var baseResponse = JsonConvert.DeserializeObject<CreditRegistrationSubjectResponse>(response);
            var isSuccess = baseResponse?.Result?.Code is CreditBureauResultCodes.SUCCESS_00000 or CreditBureauResultCodes.SUCCESS_05000;
            var ciStatus = (byte)(isSuccess ? 1 : 2);
            var token = baseResponse?.Response?.KatmSir;
            var message = baseResponse?.Result?.Message ?? (isSuccess ? "Success" : "Unknown error");

            if (isSuccess)
            {
                ci002Success++;
                _logger.LogInformation(
                    "LoanKey:{LoanKey} CI-002 success. KatmSir:{KatmSir}",
                    entityRequest.LoanKey,
                    token);
                _logWriter.Log(
                    "CreditRegistrationEntity.txt",
                    $"LoanKey:{entityRequest.LoanKey} CI-002 success. KatmSir:\t{token}");
            }
            else
            {
                ci002Error++;
                _logger.LogError("LoanKey:{LoanKey} CI-002 error. Response:{Response}", entityRequest.LoanKey, response);
                _logWriter.Log("CreditRegistrationEntity.txt", $"LoanKey:{entityRequest.LoanKey} CI-002 error. Response:{response}");
            }

            await _creditBureauReportRepository.UpsertCiStatusAsync(
                entityRequest.LoanKey,
                2,
                ciStatus,
                message,
                token,
                cancellationToken);
        }
        _logger.LogInformation(
            "CI-002 completed. Processed={Processed}, Success={Success}, Error={Error}",
            ci002Processed,
            ci002Success,
            ci002Error);

        // 3. Отклонение заявки (003)
        var declineRequests = await _creditBureauReportRepository.GetCreditRegistrationDeclineRequestsAsync(cancellationToken);
        _logger.LogInformation("CI-003 queue loaded. Count={Count}", declineRequests.Count);
        foreach (var declineRequest in declineRequests)
        {
            ci003Processed++;
            var request = declineRequest.Request;
            if (request is null)
            {
                ci003Error++;
                const string nullRequestMessage = "CI-003 request is null";
                _logger.LogWarning("CI-003 skipped due to null request. LoanKey={LoanKey}", declineRequest.LoanKey);
                await _creditBureauReportRepository.UpsertCiStatusAsync(
                    declineRequest.LoanKey,
                    3,
                    2,
                    nullRequestMessage,
                    null,
                    cancellationToken);
                continue;
            }

            request.PHead = _asokiReportApiOptions.PHead;
            request.PCode = _asokiReportApiOptions.PCode;
            request.PDate = string.IsNullOrWhiteSpace(request.PDate) ? FormatKatmDate(DateTimeOffset.Now) : request.PDate.Trim();
            var baseRequestForCredit = new BaseRequest<CreditRegistrationDeclineRequest>()
            {
                Data = request,
                Security = _requestSecurity
            };

            var response = await _requestManagerService.SendPostRequest(
                _asokiApplicationApiOptions.HostAddress + _asokiApplicationApiOptions.DeclineApplicationUrl,
                baseRequestForCredit.ToJSON(),
                declineRequest.LoanKey,
                cancellationToken);

            if (string.IsNullOrWhiteSpace(response))
            {
                ci003Error++;
                const string emptyResponseMessage = "CI-003 returned empty response";
                await _creditBureauReportRepository.UpsertCiStatusAsync(
                    declineRequest.LoanKey,
                    3,
                    2,
                    emptyResponseMessage,
                    null,
                    cancellationToken);
                continue;
            }

            var wrappedResponse = JsonConvert.DeserializeObject<BaseResponse<CreditRegistrationResponse>>(response);
            var baseResponse = wrappedResponse?.data ?? JsonConvert.DeserializeObject<CreditRegistrationResponse>(response);
            var isSuccess = baseResponse?.result is CreditBureauResultCodes.SUCCESS_00000 or CreditBureauResultCodes.SUCCESS_05000;
            var message = baseResponse?.resultMessage ?? wrappedResponse?.errorMessage ?? (isSuccess ? "Success" : "Unknown error");
            string? token = null;
            var ciStatus = (byte)(isSuccess ? 1 : 2);
            if (isSuccess)
            {
                ci003Success++;
                _logger.LogInformation("LoanKey:{LoanKey} CI-003 success. Message:{Message}", declineRequest.LoanKey, message);
                _logWriter.Log("CreditRegistrationDecline.txt", $"LoanKey:{declineRequest.LoanKey} CI-003 success. Message:{message}");
            }
            else
            {
                ci003Error++;
                _logger.LogError("LoanKey:{LoanKey} CI-003 error. Response:{Response}", declineRequest.LoanKey, response);
                _logWriter.Log("CreditRegistrationDecline.txt", $"LoanKey:{declineRequest.LoanKey} CI-003 error. Response:{response}");
            }

            await _creditBureauReportRepository.UpsertCiStatusAsync(
                declineRequest.LoanKey,
                3,
                ciStatus,
                message,
                token,
                cancellationToken);
        }
        _logger.LogInformation(
            "CI-003 completed. Processed={Processed}, Success={Success}, Error={Error}",
            ci003Processed,
            ci003Success,
            ci003Error);

        // 4. Сведениеи о кредитном договоре (004)
        var creditRegistrationRequests = await _creditBureauReportRepository.GetCreditRegistrationRequestsAsync(cancellationToken);
        _logger.LogInformation("CI-004 queue loaded. Count={Count}", creditRegistrationRequests.Count);
        foreach (var creditRegistrationRequest in creditRegistrationRequests)
        {
            ci004Processed++;
            var request = creditRegistrationRequest.Request;
            if (request is null)
            {
                ci004Error++;
                const string nullRequestMessage = "CI-004 request is null";
                _logger.LogWarning("CI-004 skipped due to null request. LoanKey={LoanKey}", creditRegistrationRequest.LoanKey);
                await _creditBureauReportRepository.UpsertCiStatusAsync(
                    creditRegistrationRequest.LoanKey,
                    4,
                    2,
                    nullRequestMessage,
                    null,
                    cancellationToken);
                continue;
            }
            request.PHead = _asokiReportApiOptions.PHead;
            request.PCode = _asokiReportApiOptions.PCode;
            request.PDate = string.IsNullOrWhiteSpace(request.PDate) ? FormatKatmDate(DateTimeOffset.Now) : request.PDate.Trim();
            var baseRequestForCredit = new BaseRequest<CreditRegistrationRequest>()
            {
                Data = request,
                Security = _requestSecurity
            };

            var response = await _requestManagerService.SendPostRequest(
                _asokiApplicationApiOptions.HostAddress + _asokiApplicationApiOptions.CreditRegistrationUrl,
                baseRequestForCredit.ToJSON(),
                creditRegistrationRequest.LoanKey,
                cancellationToken);

            if (string.IsNullOrWhiteSpace(response))
            {
                ci004Error++;
                const string emptyResponseMessage = "CI-004 returned empty response";
                await _creditBureauReportRepository.UpsertCiStatusAsync(
                    creditRegistrationRequest.LoanKey,
                    4,
                    2,
                    emptyResponseMessage,
                    null,
                    cancellationToken);
                continue;
            }

            var wrappedResponse = JsonConvert.DeserializeObject<BaseResponse<CreditRegistrationResponse>>(response);
            var baseResponse = wrappedResponse?.data ?? JsonConvert.DeserializeObject<CreditRegistrationResponse>(response);
            var isSuccess = baseResponse?.result is CreditBureauResultCodes.SUCCESS_00000 or CreditBureauResultCodes.SUCCESS_05000;
            var message = baseResponse?.resultMessage ?? wrappedResponse?.errorMessage ?? (isSuccess ? "Success" : "Unknown error");
            string? token = null;
            var ciStatus = (byte)(isSuccess ? 1 : 2);
            if (isSuccess)
            {
                ci004Success++;
                _logger.LogInformation("LoanKey:{LoanKey} CI-004 success. Message:{Message}", creditRegistrationRequest.LoanKey, message);
                _logWriter.Log("CreditRegistrationAgreement.txt", $"LoanKey:{creditRegistrationRequest.LoanKey} CI-004 success. Message:{message}");
            }
            else
            {
                ci004Error++;
                _logger.LogError("LoanKey:{LoanKey} CI-004 error. Response:{Response}", creditRegistrationRequest.LoanKey, response);
                _logWriter.Log("CreditRegistrationAgreement.txt", $"LoanKey:{creditRegistrationRequest.LoanKey} CI-004 error. Response:{response}");
            }

            await _creditBureauReportRepository.UpsertCiStatusAsync(
                creditRegistrationRequest.LoanKey,
                4,
                ciStatus,
                message,
                token,
                cancellationToken);
        }
        _logger.LogInformation(
            "CI-004 completed. Processed={Processed}, Success={Success}, Error={Error}",
            ci004Processed,
            ci004Success,
            ci004Error);

        // 5. Сведение о графике погашения кредитного договора (005)
        var repaymentSchedules = await _creditBureauReportRepository.CreditRegistrationRepaymentSchedulesAsync(cancellationToken);
        _logger.LogInformation("CI-005 queue loaded. Count={Count}", repaymentSchedules.Count);
        foreach (var repaymentSchedule in repaymentSchedules)
        {
            ci005Processed++;
            var request = repaymentSchedule.Request;
            if (request is null)
            {
                ci005Error++;
                const string nullRequestMessage = "CI-005 request is null";
                _logger.LogWarning("CI-005 skipped due to null request. LoanKey={LoanKey}", repaymentSchedule.LoanKey);
                await _creditBureauReportRepository.UpsertCiStatusAsync(
                    repaymentSchedule.LoanKey,
                    5,
                    2,
                    nullRequestMessage,
                    null,
                    cancellationToken);
                continue;
            }
            request.PHead = _asokiReportApiOptions.PHead;
            request.PCode = _asokiReportApiOptions.PCode;
            request.PDate = string.IsNullOrWhiteSpace(request.PDate) ? FormatKatmDate(DateTimeOffset.Now) : request.PDate.Trim();
            var baseRequestForCredit = new BaseRequest<CreditRegistrationRepaymentSchedule>()
            {
                Data = request,
                Security = _requestSecurity
            };

            var response = await _requestManagerService.SendPostRequest(
                _asokiApplicationApiOptions.HostAddress + _asokiApplicationApiOptions.CreditScheduleUrl,
                baseRequestForCredit.ToJSON(),
                repaymentSchedule.LoanKey,
                cancellationToken);

            if (string.IsNullOrWhiteSpace(response))
            {
                ci005Error++;
                const string emptyResponseMessage = "CI-005 returned empty response";
                await _creditBureauReportRepository.UpsertCiStatusAsync(
                    repaymentSchedule.LoanKey,
                    5,
                    2,
                    emptyResponseMessage,
                    null,
                    cancellationToken);
                continue;
            }

            var wrappedResponse = JsonConvert.DeserializeObject<BaseResponse<CreditRegistrationResponse>>(response);
            var baseResponse = wrappedResponse?.data ?? JsonConvert.DeserializeObject<CreditRegistrationResponse>(response);
            var isSuccess = baseResponse?.result is CreditBureauResultCodes.SUCCESS_00000 or CreditBureauResultCodes.SUCCESS_05000;
            var message = baseResponse?.resultMessage ?? wrappedResponse?.errorMessage ?? (isSuccess ? "Success" : "Unknown error");
            string? token = null;
            var ciStatus = (byte)(isSuccess ? 1 : 2);
            if (isSuccess)
            {
                ci005Success++;
                _logger.LogInformation("LoanKey:{LoanKey} CI-005 success. Message:{Message}", repaymentSchedule.LoanKey, message);
                _logWriter.Log("CreditRegistrationAgreement.txt", $"LoanKey:{repaymentSchedule.LoanKey} CI-005 success. Message:{message}");
            }
            else
            {
                ci005Error++;
                _logger.LogError("LoanKey:{LoanKey} CI-004 error. Response:{Response}", repaymentSchedule.LoanKey, response);
                _logWriter.Log("CreditRegistrationAgreement.txt", $"LoanKey:{repaymentSchedule.LoanKey} CI-005 error. Response:{response}");
            }

            await _creditBureauReportRepository.UpsertCiStatusAsync(
                repaymentSchedule.LoanKey,
                5,
                ciStatus,
                message,
                token,
                cancellationToken);
        }
        _logger.LogInformation(
            "CI-005 completed. Processed={Processed}, Success={Success}, Error={Error}",
            ci005Processed,
            ci005Success,
            ci005Error);

        // ── CI-011: Сведения о лизинговых договорах ───────────────────────────────
        // var leasingRequests = await _creditBureauReportRepository.GetCreditRegistrationLeasingRequestsAsync(cancellationToken);
        // _logger.LogInformation("CI-011 queue loaded. Count={Count}", leasingRequests.Count);
        // foreach (var leasingItem in leasingRequests)
        // {
        //     ci011Processed++;
        //     var request = leasingItem.Request;
        //
        //     if (request is null)
        //     {
        //         ci011Error++;
        //         _logger.LogWarning("CI-011 skipped due to null request. LoanKey={LoanKey}", leasingItem.LoanKey);
        //         await _creditBureauReportRepository.UpsertCiStatusAsync(
        //             leasingItem.LoanKey, 11, 2, "CI-011 request is null", null, cancellationToken);
        //         continue;
        //     }
        //
        //     request.PHead = _asokiReportApiOptions.PHead;
        //     request.PCode = _asokiReportApiOptions.PCode;
        //     request.PDate = string.IsNullOrWhiteSpace(request.PDate)
        //         ? FormatKatmDate(DateTimeOffset.Now)
        //         : request.PDate.Trim();
        //
        //     var baseRequest = new BaseRequest<CreditRegistrationLeasingRequest>
        //     {
        //         Data = request,
        //         Security = _requestSecurity
        //     };
        //
        //     var response = await _requestManagerService.SendPostRequest(
        //         _asokiApplicationApiOptions.HostAddress + _asokiApplicationApiOptions.CreditLeasingUrl,
        //         baseRequest.ToJSON(),
        //         leasingItem.LoanKey,
        //         cancellationToken);
        //
        //     if (string.IsNullOrWhiteSpace(response))
        //     {
        //         ci011Error++;
        //         _logger.LogError("CI-011 empty response. LoanKey={LoanKey}", leasingItem.LoanKey);
        //         await _creditBureauReportRepository.UpsertCiStatusAsync(
        //             leasingItem.LoanKey, 11, 2, "CI-011 returned empty response", null, cancellationToken);
        //         continue;
        //     }
        //
        //     var wrappedResponse = JsonConvert.DeserializeObject<BaseResponse<CreditRegistrationResponse>>(response);
        //     var baseResponse = wrappedResponse?.data ?? JsonConvert.DeserializeObject<CreditRegistrationResponse>(response);
        //     var isSuccess = baseResponse?.result is CreditBureauResultCodes.SUCCESS_00000
        //                                                or CreditBureauResultCodes.SUCCESS_05000;
        //     var message = baseResponse?.resultMessage ?? wrappedResponse?.errorMessage
        //                    ?? (isSuccess ? "Success" : "Unknown error");
        //     var ciStatus = (byte)(isSuccess ? 1 : 2);
        //
        //     if (isSuccess)
        //     {
        //         ci011Success++;
        //         _logger.LogInformation("LoanKey:{LoanKey} CI-011 success.", leasingItem.LoanKey);
        //         _logWriter.Log("CreditRegistrationLeasing.txt",
        //             $"LoanKey:{leasingItem.LoanKey} CI-011 success.");
        //     }
        //     else
        //     {
        //         ci011Error++;
        //         _logger.LogError("LoanKey:{LoanKey} CI-011 error. Response:{Response}",
        //             leasingItem.LoanKey, response);
        //         _logWriter.Log("CreditRegistrationLeasing.txt",
        //             $"LoanKey:{leasingItem.LoanKey} CI-011 error. Response:{response}");
        //     }
        //
        //     await _creditBureauReportRepository.UpsertCiStatusAsync(
        //         leasingItem.LoanKey, 11, ciStatus, message, null, cancellationToken);
        // }
        // _logger.LogInformation(
        //     "CI-011 completed. Processed={Processed}, Success={Success}, Error={Error}",
        //     ci011Processed, ci011Success, ci011Error);
        //
        // // ── CI-012: Сведения о графике погашения лизингового договора ─────────────
        // var leasingSchedules = await _creditBureauReportRepository.GetLeasingRepaymentSchedulesAsync(cancellationToken);
        // _logger.LogInformation("CI-012 queue loaded. Count={Count}", leasingSchedules.Count);
        // foreach (var leasingSchedule in leasingSchedules)
        // {
        //     ci012Processed++;
        //     var request = leasingSchedule.Request;
        //
        //     if (request is null)
        //     {
        //         ci012Error++;
        //         _logger.LogWarning("CI-012 skipped due to null request. LoanKey={LoanKey}", leasingSchedule.LoanKey);
        //         await _creditBureauReportRepository.UpsertCiStatusAsync(
        //             leasingSchedule.LoanKey, 12, 2, "CI-012 request is null", null, cancellationToken);
        //         continue;
        //     }
        //
        //     request.PHead = _asokiReportApiOptions.PHead;
        //     request.PCode = _asokiReportApiOptions.PCode;
        //     request.PDate = string.IsNullOrWhiteSpace(request.PDate)
        //         ? FormatKatmDate(DateTimeOffset.Now)
        //         : request.PDate.Trim();
        //
        //     var baseRequest = new BaseRequest<CreditRegistrationLeasingRepaymentSchedule>
        //     {
        //         Data = request,
        //         Security = _requestSecurity
        //     };
        //
        //     var response = await _requestManagerService.SendPostRequest(
        //         _asokiApplicationApiOptions.HostAddress + _asokiApplicationApiOptions.LeasingScheduleUrl,
        //         baseRequest.ToJSON(),
        //         leasingSchedule.LoanKey,
        //         cancellationToken);
        //
        //     if (string.IsNullOrWhiteSpace(response))
        //     {
        //         ci012Error++;
        //         _logger.LogError("CI-012 empty response. LoanKey={LoanKey}", leasingSchedule.LoanKey);
        //         await _creditBureauReportRepository.UpsertCiStatusAsync(
        //             leasingSchedule.LoanKey, 12, 2, "CI-012 returned empty response", null, cancellationToken);
        //         continue;
        //     }
        //
        //     var wrappedResponse = JsonConvert.DeserializeObject<BaseResponse<CreditRegistrationResponse>>(response);
        //     var baseResponse = wrappedResponse?.data ?? JsonConvert.DeserializeObject<CreditRegistrationResponse>(response);
        //     var isSuccess = baseResponse?.result is CreditBureauResultCodes.SUCCESS_00000 or CreditBureauResultCodes.SUCCESS_05000;
        //     var message = baseResponse?.resultMessage ?? wrappedResponse?.errorMessage ?? (isSuccess ? "Success" : "Unknown error");
        //     var ciStatus = (byte)(isSuccess ? 1 : 2);
        //
        //     if (isSuccess)
        //     {
        //         ci012Success++;
        //         _logger.LogInformation("LoanKey:{LoanKey} CI-012 success. Message:{Message}", leasingSchedule.LoanKey, message);
        //         _logWriter.Log("CreditRegistrationAgreement.txt", $"LoanKey:{leasingSchedule.LoanKey} CI-012 success. Message:{message}");
        //     }
        //     else
        //     {
        //         ci012Error++;
        //         _logger.LogError("LoanKey:{LoanKey} CI-012 error. Response:{Response}", leasingSchedule.LoanKey, response);
        //         _logWriter.Log("CreditRegistrationAgreement.txt", $"LoanKey:{leasingSchedule.LoanKey} CI-012 error. Response:{response}");
        //     }
        //
        //     await _creditBureauReportRepository.UpsertCiStatusAsync(
        //         leasingSchedule.LoanKey, 12, ciStatus, message, null, cancellationToken);
        // }
        // _logger.LogInformation(
        //     "CI-012 completed. Processed={Processed}, Success={Success}, Error={Error}",
        //     ci012Processed, ci012Success, ci012Error);
        //
        // // ── CI-013: Сведения об объекте лизингового договора ─────────────────────
        // var leasingObjects = await _creditBureauReportRepository.GetLeasingRepaymentObjectsAsync(cancellationToken);
        // _logger.LogInformation("CI-013 queue loaded. Count={Count}", leasingObjects.Count);
        // foreach (var leasingObject in leasingObjects)
        // {
        //     ci013Processed++;
        //     var request = leasingObject.Request;
        //
        //     if (request is null)
        //     {
        //         ci013Error++;
        //         _logger.LogWarning("CI-013 skipped due to null request. LoanKey={LoanKey}", leasingObject.LoanKey);
        //         await _creditBureauReportRepository.UpsertCiStatusAsync(
        //             leasingObject.LoanKey, 13, 2, "CI-013 request is null", null, cancellationToken);
        //         continue;
        //     }
        //
        //     request.PHead = _asokiReportApiOptions.PHead;
        //     request.PCode = _asokiReportApiOptions.PCode;
        //     request.PDate = string.IsNullOrWhiteSpace(request.PDate)
        //         ? FormatKatmDate(DateTimeOffset.Now)
        //         : request.PDate.Trim();
        //
        //     var baseRequest = new BaseRequest<CreditRegistrationLeasingRepayment>
        //     {
        //         Data = request,
        //         Security = _requestSecurity
        //     };
        //
        //     var response = await _requestManagerService.SendPostRequest(
        //         _asokiApplicationApiOptions.HostAddress + _asokiApplicationApiOptions.LeasingRepaymentUrl,
        //         baseRequest.ToJSON(),
        //         leasingObject.LoanKey,
        //         cancellationToken);
        //
        //     if (string.IsNullOrWhiteSpace(response))
        //     {
        //         ci013Error++;
        //         _logger.LogError("CI-013 empty response. LoanKey={LoanKey}", leasingObject.LoanKey);
        //         await _creditBureauReportRepository.UpsertCiStatusAsync(
        //             leasingObject.LoanKey, 13, 2, "CI-013 returned empty response", null, cancellationToken);
        //         continue;
        //     }
        //
        //     var wrappedResponse = JsonConvert.DeserializeObject<BaseResponse<CreditRegistrationResponse>>(response);
        //     var baseResponse = wrappedResponse?.data ?? JsonConvert.DeserializeObject<CreditRegistrationResponse>(response);
        //     var isSuccess = baseResponse?.result is CreditBureauResultCodes.SUCCESS_00000 or CreditBureauResultCodes.SUCCESS_05000;
        //     var message = baseResponse?.resultMessage ?? wrappedResponse?.errorMessage ?? (isSuccess ? "Success" : "Unknown error");
        //     var ciStatus = (byte)(isSuccess ? 1 : 2);
        //
        //     if (isSuccess)
        //     {
        //         ci013Success++;
        //         _logger.LogInformation("LoanKey:{LoanKey} CI-013 success. Message:{Message}", leasingObject.LoanKey, message);
        //         _logWriter.Log("CreditRegistrationAgreement.txt", $"LoanKey:{leasingObject.LoanKey} CI-013 success. Message:{message}");
        //     }
        //     else
        //     {
        //         ci013Error++;
        //         _logger.LogError("LoanKey:{LoanKey} CI-013 error. Response:{Response}", leasingObject.LoanKey, response);
        //         _logWriter.Log("CreditRegistrationAgreement.txt", $"LoanKey:{leasingObject.LoanKey} CI-013 error. Response:{response}");
        //     }
        //
        //     await _creditBureauReportRepository.UpsertCiStatusAsync(
        //         leasingObject.LoanKey, 13, ciStatus, message, null, cancellationToken);
        // }
        // _logger.LogInformation(
        //     "CI-013 completed. Processed={Processed}, Success={Success}, Error={Error}",
        //     ci013Processed, ci013Success, ci013Error);
        //
        // // ── CI-014: Сведения о договорах факторинга ───────────────────────────────
        // var factoringRequests = await _creditBureauReportRepository.GetCreditRegistrationFactoringRequestsAsync(cancellationToken);
        // _logger.LogInformation("CI-014 queue loaded. Count={Count}", factoringRequests.Count);
        // foreach (var factoringItem in factoringRequests)
        // {
        //     ci014Processed++;
        //     var request = factoringItem.Request;
        //
        //     if (request is null)
        //     {
        //         ci014Error++;
        //         _logger.LogWarning("CI-014 skipped due to null request. LoanKey={LoanKey}", factoringItem.LoanKey);
        //         await _creditBureauReportRepository.UpsertCiStatusAsync(
        //             factoringItem.LoanKey, 14, 2, "CI-014 request is null", null, cancellationToken);
        //         continue;
        //     }
        //
        //     request.PHead = _asokiReportApiOptions.PHead;
        //     request.PCode = _asokiReportApiOptions.PCode;
        //     request.PDate = string.IsNullOrWhiteSpace(request.PDate)
        //         ? FormatKatmDate(DateTimeOffset.Now)
        //         : request.PDate.Trim();
        //     request.PStartDate = string.IsNullOrWhiteSpace(request.PStartDate)
        //         ? FormatKatmDate(DateTimeOffset.Now)
        //         : request.PStartDate.Trim();
        //     request.PEndDate = string.IsNullOrWhiteSpace(request.PEndDate)
        //         ? FormatKatmDate(DateTimeOffset.Now)
        //         : request.PEndDate.Trim();
        //
        //     var baseRequest = new BaseRequest<CreditRegistrationFactoring>
        //     {
        //         Data = request,
        //         Security = _requestSecurity
        //     };
        //
        //     var response = await _requestManagerService.SendPostRequest(
        //         _asokiApplicationApiOptions.HostAddress + _asokiApplicationApiOptions.CreditFactoringUrl,
        //         baseRequest.ToJSON(),
        //         factoringItem.LoanKey,
        //         cancellationToken);
        //
        //     if (string.IsNullOrWhiteSpace(response))
        //     {
        //         ci014Error++;
        //         _logger.LogError("CI-014 empty response. LoanKey={LoanKey}", factoringItem.LoanKey);
        //         await _creditBureauReportRepository.UpsertCiStatusAsync(
        //             factoringItem.LoanKey, 14, 2, "CI-014 returned empty response", null, cancellationToken);
        //         continue;
        //     }
        //
        //     var wrappedResponse = JsonConvert.DeserializeObject<BaseResponse<CreditRegistrationResponse>>(response);
        //     var baseResponse = wrappedResponse?.data ?? JsonConvert.DeserializeObject<CreditRegistrationResponse>(response);
        //     var isSuccess = baseResponse?.result is CreditBureauResultCodes.SUCCESS_00000
        //                                           or CreditBureauResultCodes.SUCCESS_05000;
        //     var message = baseResponse?.resultMessage ?? wrappedResponse?.errorMessage
        //                    ?? (isSuccess ? "Success" : "Unknown error");
        //     var ciStatus = (byte)(isSuccess ? 1 : 2);
        //
        //     if (isSuccess)
        //     {
        //         ci014Success++;
        //         _logger.LogInformation("LoanKey:{LoanKey} CI-014 success.", factoringItem.LoanKey);
        //         _logWriter.Log("CreditRegistrationFactoring.txt",
        //             $"LoanKey:{factoringItem.LoanKey} CI-014 success.");
        //     }
        //     else
        //     {
        //         ci014Error++;
        //         _logger.LogError("LoanKey:{LoanKey} CI-014 error. Response:{Response}",
        //             factoringItem.LoanKey, response);
        //         _logWriter.Log("CreditRegistrationFactoring.txt",
        //             $"LoanKey:{factoringItem.LoanKey} CI-014 error. Response:{response}");
        //     }
        //
        //     await _creditBureauReportRepository.UpsertCiStatusAsync(
        //         factoringItem.LoanKey, 14, ciStatus, message, null, cancellationToken);
        // }
        // _logger.LogInformation(
        //     "CI-014 completed. Processed={Processed}, Success={Success}, Error={Error}",
        //     ci014Processed, ci014Success, ci014Error);


        // 10. Сведения об остатках на счетах (CI-015)
        // Условие: ci004 = 1 (договор успешно зарегистрирован)
        var repaymentRequests = await _creditBureauReportRepository.GetCreditRegistrationRepaymentRequestsAsync(cancellationToken);
        _logger.LogInformation("CI-015 queue loaded. Count={Count}", repaymentRequests.Count);
        foreach (var repaymentItem in repaymentRequests)
        {
            ci015Processed++;
            var request = repaymentItem.Request;
            if (request is null)
            {
                ci015Error++;
                _logger.LogWarning("CI-015 skipped due to null request. LoanKey={LoanKey}", repaymentItem.LoanKey);
                await _creditBureauReportRepository.UpsertCiStatusAsync(
                    repaymentItem.LoanKey, 15, 2, "CI-015 request is null", null, cancellationToken);
                continue;
            }
            request.PHead = _asokiReportApiOptions.PHead;
            request.PCode = _asokiReportApiOptions.PCode;
            request.PDate = string.IsNullOrWhiteSpace(request.PDate)
                ? FormatKatmDate(DateTimeOffset.Now)
                : request.PDate.Trim();
            var BaseRequest = new BaseRequest<CreditRegistrationRepayment>
            {
                Data = request,
                Security = _requestSecurity
            };
            var response = await _requestManagerService.SendPostRequest(
                _asokiApplicationApiOptions.HostAddress + _asokiApplicationApiOptions.CreditRegistrationRepaymentUrl,
                BaseRequest.ToJSON(),
                repaymentItem.LoanKey,
                cancellationToken);
            if (string.IsNullOrWhiteSpace(response))
            {
                ci015Error++;
                _logger.LogError("CI-015 empty response. LoanKey={LoanKey}", repaymentItem.LoanKey);
                await _creditBureauReportRepository.UpsertCiStatusAsync(
                    repaymentItem.LoanKey, 15, 2, "CI-015 returned empty response", null, cancellationToken);
                continue;
            }
            var wrappedResponse = JsonConvert.DeserializeObject<BaseResponse<CreditRegistrationResponse>>(response);
            var baseResponse = wrappedResponse?.data ?? JsonConvert.DeserializeObject<CreditRegistrationResponse>(response);
            var isSuccess = baseResponse.result is CreditBureauResultCodes.SUCCESS_00000
                or CreditBureauResultCodes.SUCCESS_05000;
            var message = baseResponse?.resultMessage ?? wrappedResponse?.errorMessage ?? (isSuccess ? "Success" : "Unknown error");
            string? token = null;
            var ciStatus = (byte)(isSuccess ? 1 : 2);
            if (isSuccess)
            {
                ci015Success++;
                _logger.LogInformation("LoanKey:{LoanKey} CI-015 success. Message:{Message}", repaymentItem.LoanKey, message);
                _logWriter.Log("CreditRegistrationAgreement.txt", $"LoanKey:{repaymentItem.LoanKey} CI-015 success. Message:{message}");
            }
            else
            {
                ci015Error++;
                _logger.LogError("LoanKey:{LoanKey} CI-015 success. Message:{Message}", repaymentItem.LoanKey, response);
                _logWriter.Log("CreditRegistrationAgreement.txt", $"LoanKey:{repaymentItem.LoanKey} CI-015 error. Response:{response}");
            }

            await _creditBureauReportRepository.UpsertCiStatusAsync(
                repaymentItem.LoanKey,
                15,
                ciStatus,
                message,
                token,
                cancellationToken);

        }
        _logger.LogInformation(
            "CI-015 completed. Processed={Processed}, Success={Success}, Error={Error}",
            ci015Processed,
            ci015Success,
            ci015Error);

        // 11.  Сведения о платежных документах (о бухгалтерских операциях) для коммерческих банков (CI-016)
        // Условие: ci004 = 1 (договор успешно зарегистрирован)
        var repaymentBankDetails = await _creditBureauReportRepository.GetCreditRegistrationBankDetailsRequestsAsync(cancellationToken);
        _logger.LogInformation("CI-016 queue loaded. Count={Count}", repaymentRequests.Count);
        foreach (var repaymentBankDetail in repaymentBankDetails)
        {
            ci016Processed++;
            var request = repaymentBankDetail.Request;
            if (request is null)
            {
                ci016Error++;
                _logger.LogWarning("CI-016 skipped due to null request. LoanKey={LoanKey}", repaymentBankDetail.LoanKey);
                await _creditBureauReportRepository.UpsertCiStatusAsync(
                    repaymentBankDetail.LoanKey, 16, 2, "CI-016 request is null", null, cancellationToken);
                continue;
            }
            request.PHead = _asokiReportApiOptions.PHead;
            request.PCode = _asokiReportApiOptions.PCode;
            request.PDate = string.IsNullOrWhiteSpace(request.PDate)
                ? FormatKatmDate(DateTimeOffset.Now)
                : request.PDate.Trim();
            var BaseRequest = new BaseRequest<CreditRegistrationBankDitailRequest>
            {
                Data = request,
                Security = _requestSecurity
            };
            var response = await _requestManagerService.SendPostRequest(
                _asokiApplicationApiOptions.HostAddress + _asokiApplicationApiOptions.CreditRegistrationRepaymentBankDitailUrl,
                BaseRequest.ToJSON(),
                repaymentBankDetail.LoanKey,
                cancellationToken);
            if (string.IsNullOrWhiteSpace(response))
            {
                ci016Error++;
                _logger.LogError("CI-016 empty response. LoanKey={LoanKey}", repaymentBankDetail.LoanKey);
                await _creditBureauReportRepository.UpsertCiStatusAsync(
                    repaymentBankDetail.LoanKey, 16, 2, "CI-016 returned empty response", null, cancellationToken);
                continue;
            }
            var wrappedResponse = JsonConvert.DeserializeObject<BaseResponse<CreditRegistrationResponse>>(response);
            var baseResponse = wrappedResponse?.data ?? JsonConvert.DeserializeObject<CreditRegistrationResponse>(response);
            var isSuccess = baseResponse.result is CreditBureauResultCodes.SUCCESS_00000
                or CreditBureauResultCodes.SUCCESS_05000;
            var message = baseResponse?.resultMessage ?? wrappedResponse?.errorMessage ?? (isSuccess ? "Success" : "Unknown error");
            string? token = null;
            var ciStatus = (byte)(isSuccess ? 1 : 2);
            if (isSuccess)
            {
                ci016Success++;
                _logger.LogInformation("LoanKey:{LoanKey} CI-016 success. Message:{Message}", repaymentBankDetail.LoanKey, message);
                _logWriter.Log("CreditRegistrationAgreement.txt", $"LoanKey:{repaymentBankDetail.LoanKey} CI-016 success. Message:{message}");
            }
            else
            {
                ci016Error++;
                _logger.LogError("LoanKey:{LoanKey} CI-016 success. Message:{Message}", repaymentBankDetail.LoanKey, response);
                _logWriter.Log("CreditRegistrationAgreement.txt", $"LoanKey:{repaymentBankDetail.LoanKey} CI-016 error. Response:{response}");
            }

            await _creditBureauReportRepository.UpsertCiStatusAsync(
                repaymentBankDetail.LoanKey,
                16,
                ciStatus,
                message,
                token,
                cancellationToken);

        }
        _logger.LogInformation(
            "CI-016 completed. Processed={Processed}, Success={Success}, Error={Error}",
            ci016Processed,
            ci016Success,
            ci016Error);

        // ── CI-017: Запросы на получение кредитной информации ─────────────────────
        // var creditReportRequests = await _creditBureauReportRepository.GetCreditReportRequestsAsync(cancellationToken);
        // _logger.LogInformation("CI-017 queue loaded. Count={Count}", creditReportRequests.Count);
        // foreach (var reportItem in creditReportRequests)
        // {
        //     ci017Processed++;
        //
        //     var request = new CreditReportRequest
        //     {
        //         PHead = _asokiReportApiOptions.PHead,      // Головной код организации
        //         PCode = _asokiReportApiOptions.PCode,      // Код организации
        //         PClaimId = reportItem.PClaimId,               // Уникальный ID заявки
        //         PReportId = reportItem.PReportId,              // ID отчёта
        //         PLoanSubject = reportItem.PLoanSubject,           // Тип субъекта A18
        //         PLoanSubjectType = reportItem.PLoanSubjectType,       // Подтип субъекта A18
        //         PPin = reportItem.PPin,                   // ПИНФЛ (физлица)
        //         PTin = reportItem.PTin,                   // ИНН (юрлица)
        //         PReportFormat = reportItem.PReportFormat,          // Формат отчёта
        //         PReportReason = reportItem.PReportReason,          // Цель изучения (v9.15, M)
        //         PToken = string.IsNullOrWhiteSpace(reportItem.PToken) // KATM-SIR если есть
        //             ? null : reportItem.PToken,
        //     };
        //
        //     var baseRequest = new BaseRequest<CreditReportRequest>
        //     {
        //         Data = request,
        //         Security = _requestSecurity
        //     };
        //
        //     var response = await _requestManagerService.SendPostRequest(
        //         _asokiReportApiOptions.HostAddress + _asokiReportApiOptions.ReportUrl,
        //         baseRequest.ToJSON(),
        //         reportItem.LoanKey,
        //         cancellationToken);
        //
        //     if (string.IsNullOrWhiteSpace(response))
        //     {
        //         ci017Error++;
        //         _logger.LogError("CI-017 empty response. LoanKey={LoanKey}", reportItem.LoanKey);
        //         await _creditBureauReportRepository.UpsertCiStatusAsync(
        //             reportItem.LoanKey, 17, 2, "CI-017 returned empty response", null, cancellationToken);
        //         continue;
        //     }
        //
        //     var baseResponse = JsonConvert.DeserializeObject<BaseResponse<CreditReportResponse>>(response);
        //     var resultCode = baseResponse?.data?.result;
        //     var resultMsg = baseResponse?.data?.resultMessage ?? string.Empty;
        //
        //     if (resultCode == CreditBureauResultCodes.SUCCESS_05000)
        //     {
        //         // Отчёт получен сразу → сохраняем base64 в логах, ci017=1
        //         ci017Success++;
        //         _logger.LogInformation("LoanKey:{LoanKey} CI-017 success (immediate).", reportItem.LoanKey);
        //         _logWriter.Log("CreditReport017.txt",
        //             $"LoanKey:{reportItem.LoanKey} CI-017 success. Base64 length:{baseResponse?.data?.reportBase64?.Length}");
        //         await _creditBureauReportRepository.UpsertCiStatusAsync(
        //             reportItem.LoanKey, 17, 1, resultMsg, null, cancellationToken);
        //     }
        //     else if (resultCode == CreditBureauResultCodes.WAIT_AND_TRY_AGAIN)
        //     {
        //         // Отчёт формируется (05050) → сохраняем token, ci017=0, polling подберёт
        //         ci017Pending++;
        //         var pollingToken = baseResponse?.data?.token;
        //         _logger.LogInformation("LoanKey:{LoanKey} CI-017 pending (05050). Token:{Token}",
        //             reportItem.LoanKey, pollingToken);
        //         _logWriter.Log("CreditReport017.txt",
        //             $"LoanKey:{reportItem.LoanKey} CI-017 pending. Token:{pollingToken}");
        //         await _creditBureauReportRepository.UpsertCiStatusAsync(
        //             reportItem.LoanKey, 17, 0, "Ожидание ответа 05050", pollingToken, cancellationToken);
        //     }
        //     else
        //     {
        //         ci017Error++;
        //         _logger.LogError("LoanKey:{LoanKey} CI-017 error. Code:{Code} Response:{Response}",
        //             reportItem.LoanKey, resultCode, response);
        //         _logWriter.Log("CreditReport017.txt",
        //             $"LoanKey:{reportItem.LoanKey} CI-017 error. Response:{response}");
        //         await _creditBureauReportRepository.UpsertCiStatusAsync(
        //             reportItem.LoanKey, 17, 2, resultMsg, null, cancellationToken);
        //     }
        // }

        // _logger.LogInformation(
        //     "CI-017 completed. Processed={Processed}, Success={Success}, Pending={Pending}, Error={Error}",
        //     ci017Processed, ci017Success, ci017Pending, ci017Error);
        //
        // // ── CI-017 Polling: опрос статуса для займов с 05050 ──────────────────────
        // var pollRequests = await _creditBureauReportRepository.GetCreditReportPollRequestsAsync(cancellationToken);
        // _logger.LogInformation("CI-017 Poll queue loaded. Count={Count}", pollRequests.Count);
        // foreach (var pollItem in pollRequests)
        // {
        //     var statusRequest = new CreditReportStatusRequest
        //     {
        //         pHead = _asokiReportApiOptions.PHead,  // Головной код организации
        //         pCode = _asokiReportApiOptions.PCode,  // Код организации
        //         pClaimId = pollItem.PClaimId,             // Уникальный ID заявки
        //         pToken = pollItem.PToken!,              // Токен от 05050
        //         pReportFormat = pollItem.PReportFormat,        // Формат отчёта
        //     };
        //
        //     var baseStatusRequest = new BaseRequest<CreditReportStatusRequest>
        //     {
        //         Data = statusRequest,
        //         Security = _requestSecurity
        //     };
        //
        //     var response = await _requestManagerService.SendPostRequest(
        //         _asokiReportApiOptions.HostAddress + _asokiReportApiOptions.ReportStatusUrl,
        //         baseStatusRequest.ToJSON(),
        //         pollItem.LoanKey,
        //         cancellationToken);
        //
        //     if (string.IsNullOrWhiteSpace(response))
        //     {
        //         _logger.LogWarning("CI-017 Poll empty response. LoanKey={LoanKey}", pollItem.LoanKey);
        //         continue; // не меняем статус — попробуем в следующем цикле
        //     }
        //
        //     var baseResponse = JsonConvert.DeserializeObject<BaseResponse<CreditReportStatusResponse>>(response);
        //     var resultCode = baseResponse?.data?.result;
        //     var resultMsg = baseResponse?.data?.resultMessage ?? string.Empty;
        //
        //     if (resultCode == CreditBureauResultCodes.SUCCESS_05000)
        //     {
        //         _logger.LogInformation("LoanKey:{LoanKey} CI-017 Poll success.", pollItem.LoanKey);
        //         _logWriter.Log("CreditReport017.txt",
        //             $"LoanKey:{pollItem.LoanKey} CI-017 Poll success. Base64 length:{baseResponse?.data?.reportBase64?.Length}");
        //         await _creditBureauReportRepository.UpsertCiStatusAsync(
        //             pollItem.LoanKey, 17, 1, resultMsg, null, cancellationToken);
        //     }
        //     else if (resultCode == CreditBureauResultCodes.WAIT_AND_TRY_AGAIN)
        //     {
        //         // Ещё ждём — оставляем ci017=0, token остаётся
        //         _logger.LogInformation("LoanKey:{LoanKey} CI-017 Poll still waiting.", pollItem.LoanKey);
        //     }
        //     else
        //     {
        //         _logger.LogError("LoanKey:{LoanKey} CI-017 Poll error. Code:{Code}",
        //             pollItem.LoanKey, resultCode);
        //         _logWriter.Log("CreditReport017.txt",
        //             $"LoanKey:{pollItem.LoanKey} CI-017 Poll error. Response:{response}");
        //         await _creditBureauReportRepository.UpsertCiStatusAsync(
        //             pollItem.LoanKey, 17, 2, resultMsg, null, cancellationToken);
        //     }
        // }
        // _logger.LogInformation("CI-017 Poll completed. Count={Count}", pollRequests.Count);


        // 14. Сведения о статусе счетов (CI-018) 
        var accountStatusRequests = await _creditBureauReportRepository.GetAccountStatusRequestsAsync(cancellationToken);
        _logger.LogInformation("CI-018 queue loaded. Count={Count}", accountStatusRequests.Count);
        foreach (var accountStatusItem in accountStatusRequests)
        {
            ci018Processed++;
            var request = accountStatusItem.Request;

            if (request is null)
            {
                ci018Error++;
                _logger.LogWarning("CI-018 skipped due to null request. LoanKey={LoanKey}", accountStatusItem.LoanKey);
                await _creditBureauReportRepository.UpsertCiStatusAsync(
                    accountStatusItem.LoanKey, 18, 2, "CI-018 request is null", null, cancellationToken);
                continue;
            }

            request.PHead = _asokiReportApiOptions.PHead;
            request.PCode = _asokiReportApiOptions.PCode;
            request.PDate = FormatKatmDate(DateTimeOffset.Now);

            var baseRequest = new BaseRequest<CreditRegistrationAccountStatus>
            {
                Data = request,
                Security = _requestSecurity
            };

            var response = await _requestManagerService.SendPostRequest(
                _asokiApplicationApiOptions.HostAddress + _asokiApplicationApiOptions.AccountStatusUrl,
                baseRequest.ToJSON(),
                accountStatusItem.LoanKey,
                cancellationToken);

            if (string.IsNullOrWhiteSpace(response))
            {
                ci018Error++;
                _logger.LogError("CI-018 empty response. LoanKey={LoanKey}", accountStatusItem.LoanKey);
                await _creditBureauReportRepository.UpsertCiStatusAsync(
                    accountStatusItem.LoanKey, 18, 2, "CI-018 returned empty response", null, cancellationToken);
                continue;
            }

            var wrappedResponse = JsonConvert.DeserializeObject<BaseResponse<CreditRegistrationResponse>>(response);
            var baseResponse = wrappedResponse?.data ?? JsonConvert.DeserializeObject<CreditRegistrationResponse>(response);
            var isSuccess = baseResponse?.result is CreditBureauResultCodes.SUCCESS_00000
                                                       or CreditBureauResultCodes.SUCCESS_05000;
            var message = baseResponse?.resultMessage ?? wrappedResponse?.errorMessage
                           ?? (isSuccess ? "Success" : "Unknown error");
            var ciStatus = (byte)(isSuccess ? 1 : 2);

            if (isSuccess)
            {
                ci018Success++;
                _logger.LogInformation("LoanKey:{LoanKey} CI-018 success.", accountStatusItem.LoanKey);
                _logWriter.Log("CreditRegistrationAccountStatus.txt",
                    $"LoanKey:{accountStatusItem.LoanKey} CI-018 success.");
            }
            else
            {
                ci018Error++;
                _logger.LogError("LoanKey:{LoanKey} CI-018 error. Response:{Response}",
                    accountStatusItem.LoanKey, response);
                _logWriter.Log("CreditRegistrationAccountStatus.txt",
                    $"LoanKey:{accountStatusItem.LoanKey} CI-018 error. Response:{response}");
            }

            await _creditBureauReportRepository.UpsertCiStatusAsync(
                accountStatusItem.LoanKey, 18, ciStatus, message, null, cancellationToken);
        }
        _logger.LogInformation(
            "CI-018 completed. Processed={Processed}, Success={Success}, Error={Error}",
            ci018Processed, ci018Success, ci018Error);

        // 20. Сведения о владельце залога (020)
        var customerOwners = await _creditBureauReportRepository.CreditRegistrationPledgeOwnerAsync(cancellationToken);
        _logger.LogInformation("CI-020 queue loaded. Count={Count}", customerOwners.Count);
        foreach (var customerOwner in customerOwners)
        {
            ci020Processed++;
            var request = customerOwner.Request;
            if (request is null)
            {
                ci020Error++;
                const string nullRequestMessage = "CI-020 request is null";
                _logger.LogWarning("CI-020 skipped due to null request. LoanKey={LoanKey}", customerOwner.LoanKey);
                await _creditBureauReportRepository.UpsertCiStatusAsync(
                    customerOwner.LoanKey,
                    20,
                    2,
                    nullRequestMessage,
                    null,
                    cancellationToken);
                continue;
            }
            request.PHead = _asokiReportApiOptions.PHead;
            request.PCode = _asokiReportApiOptions.PCode;
            request.PDate = string.IsNullOrWhiteSpace(request.PDate) ? FormatKatmDate(DateTimeOffset.Now) : request.PDate.Trim();
            var baseRequestForCredit = new BaseRequest<CreditRegistrationPledgeOwner>()
            {
                Data = request,
                Security = _requestSecurity
            };

            var response = await _requestManagerService.SendPostRequest(
                _asokiApplicationApiOptions.HostAddress + _asokiApplicationApiOptions.CreditPledgeOwnerUrl,
                baseRequestForCredit.ToJSON(),
                customerOwner.LoanKey,
                cancellationToken);

            if (string.IsNullOrWhiteSpace(response))
            {
                ci020Error++;
                const string emptyResponseMessage = "CI-020 returned empty response";
                await _creditBureauReportRepository.UpsertCiStatusAsync(
                    customerOwner.LoanKey,
                    20,
                    2,
                    emptyResponseMessage,
                    null,
                    cancellationToken);
                continue;
            }

            var wrappedResponse = JsonConvert.DeserializeObject<BaseResponse<CreditRegistrationResponse>>(response);
            var baseResponse = wrappedResponse?.data ?? JsonConvert.DeserializeObject<CreditRegistrationResponse>(response);
            var isSuccess = baseResponse?.result is CreditBureauResultCodes.SUCCESS_00000 or CreditBureauResultCodes.SUCCESS_05000;
            var message = baseResponse?.resultMessage ?? wrappedResponse?.errorMessage ?? (isSuccess ? "Success" : "Unknown error");
            string? token = null;
            var ciStatus = (byte)(isSuccess ? 1 : 2);
            if (isSuccess)
            {
                ci020Success++;
                _logger.LogInformation("LoanKey:{LoanKey} CI-020 success. Message:{Message}", customerOwner.LoanKey, message);
                _logWriter.Log("CreditRegistrationAgreement.txt", $"LoanKey:{customerOwner.LoanKey} CI-020 success. Message:{message}");
            }
            else
            {
                ci020Error++;
                _logger.LogError("LoanKey:{LoanKey} CI-020 error. Response:{Response}", customerOwner.LoanKey, response);
                _logWriter.Log("CreditRegistrationAgreement.txt", $"LoanKey:{customerOwner.LoanKey} CI-020 error. Response:{response}");
            }

            await _creditBureauReportRepository.UpsertCiStatusAsync(
                customerOwner.LoanKey,
                20,
                ciStatus,
                message,
                token,
                cancellationToken);
        }
        _logger.LogInformation(
            "CI-020 completed. Processed={Processed}, Success={Success}, Error={Error}",
            ci020Processed,
            ci020Success,
            ci020Error);

        // ── CI-021: Сведения об обеспечении кредита ──────────────────────────────
        var pledgeSecurityRequests = await _creditBureauReportRepository.GetPledgeSecurityRequestsAsync(cancellationToken);
        _logger.LogInformation("CI-021 queue loaded. Count={Count}", pledgeSecurityRequests.Count);
        foreach (var pledgeSecurityItem in pledgeSecurityRequests)
        {
            ci021Processed++;
            var request = pledgeSecurityItem.Request;

            if (request is null)
            {
                ci021Error++;
                _logger.LogWarning("CI-021 skipped due to null request. LoanKey={LoanKey}", pledgeSecurityItem.LoanKey);
                await _creditBureauReportRepository.UpsertCiStatusAsync(
                    pledgeSecurityItem.LoanKey, 21, 2, "CI-021 request is null", null, cancellationToken);
                continue;
            }

            request.PHead = _asokiReportApiOptions.PHead;
            request.PCode = _asokiReportApiOptions.PCode;
            request.PDate = string.IsNullOrWhiteSpace(request.PDate)
                ? FormatKatmDate(DateTimeOffset.Now)
                : request.PDate.Trim();

            var baseRequest = new BaseRequest<CreditRegistrationPledgeSecurity>
            {
                Data = request,
                Security = _requestSecurity
            };

            var response = await _requestManagerService.SendPostRequest(
                _asokiApplicationApiOptions.HostAddress + _asokiApplicationApiOptions.CreditPledgeSecurityUrl,
                baseRequest.ToJSON(),
                pledgeSecurityItem.LoanKey,
                cancellationToken);

            if (string.IsNullOrWhiteSpace(response))
            {
                ci021Error++;
                _logger.LogError("CI-021 empty response. LoanKey={LoanKey}", pledgeSecurityItem.LoanKey);
                await _creditBureauReportRepository.UpsertCiStatusAsync(
                    pledgeSecurityItem.LoanKey, 21, 2, "CI-021 returned empty response", null, cancellationToken);
                continue;
            }

            var wrappedResponse = JsonConvert.DeserializeObject<BaseResponse<CreditRegistrationResponse>>(response);
            var baseResponse = wrappedResponse?.data ?? JsonConvert.DeserializeObject<CreditRegistrationResponse>(response);
            var isSuccess = baseResponse?.result is CreditBureauResultCodes.SUCCESS_00000
                                                       or CreditBureauResultCodes.SUCCESS_05000;
            var message = baseResponse?.resultMessage ?? wrappedResponse?.errorMessage
                           ?? (isSuccess ? "Success" : "Unknown error");
            var ciStatus = (byte)(isSuccess ? 1 : 2);

            if (isSuccess)
            {
                ci021Success++;
                _logger.LogInformation("LoanKey:{LoanKey} CI-021 success.", pledgeSecurityItem.LoanKey);
                _logWriter.Log("CreditRegistrationPledgeSecurity.txt",
                    $"LoanKey:{pledgeSecurityItem.LoanKey} CI-021 success.");
            }
            else
            {
                ci021Error++;
                _logger.LogError("LoanKey:{LoanKey} CI-021 error. Response:{Response}",
                    pledgeSecurityItem.LoanKey, response);
                _logWriter.Log("CreditRegistrationPledgeSecurity.txt",
                    $"LoanKey:{pledgeSecurityItem.LoanKey} CI-021 error. Response:{response}");
            }

            await _creditBureauReportRepository.UpsertCiStatusAsync(
                pledgeSecurityItem.LoanKey, 21, ciStatus, message, null, cancellationToken);
        }

        _logger.LogInformation(
            "CI-021 completed. Processed={Processed}, Success={Success}, Error={Error}",
            ci021Processed, ci021Success, ci021Error);

        // ── CI-022: Сведения о бухгалтерских проводках для хозяйствующих субъектов ──
        var businessDetailsRequests = await _creditBureauReportRepository.GetCreditRegistrationBusinessDetailsRequestsAsync(cancellationToken);
        _logger.LogInformation("CI-022 queue loaded. Count={Count}", businessDetailsRequests.Count);
        foreach (var businessItem in businessDetailsRequests)
        {
            ci022Processed++;
            var request = businessItem.Request;

            if (request is null)
            {
                ci022Error++;
                _logger.LogWarning("CI-022 skipped due to null request. LoanKey={LoanKey}", businessItem.LoanKey);
                await _creditBureauReportRepository.UpsertCiStatusAsync(
                    businessItem.LoanKey, 22, 2, "CI-022 request is null", null, cancellationToken);
                continue;
            }

            request.PHead = _asokiReportApiOptions.PHead;
            request.PCode = _asokiReportApiOptions.PCode;
            request.PDate = string.IsNullOrWhiteSpace(request.PDate)
                ? FormatKatmDate(DateTimeOffset.Now)
                : request.PDate.Trim();

            var baseRequest = new BaseRequest<CreditRegistrationBusinessDetailRequest>
            {
                Data = request,
                Security = _requestSecurity
            };

            var response = await _requestManagerService.SendPostRequest(
                _asokiApplicationApiOptions.HostAddress + _asokiApplicationApiOptions.CreditRegistrationBusinessDetailsUrl,
                baseRequest.ToJSON(),
                businessItem.LoanKey,
                cancellationToken);

            if (string.IsNullOrWhiteSpace(response))
            {
                ci022Error++;
                _logger.LogError("CI-022 empty response. LoanKey={LoanKey}", businessItem.LoanKey);
                await _creditBureauReportRepository.UpsertCiStatusAsync(
                    businessItem.LoanKey, 22, 2, "CI-022 returned empty response", null, cancellationToken);
                continue;
            }

            var wrappedResponse = JsonConvert.DeserializeObject<BaseResponse<CreditRegistrationResponse>>(response);
            var baseResponse = wrappedResponse?.data ?? JsonConvert.DeserializeObject<CreditRegistrationResponse>(response);
            var isSuccess = baseResponse?.result is CreditBureauResultCodes.SUCCESS_00000
                                                  or CreditBureauResultCodes.SUCCESS_05000;
            var message = baseResponse?.resultMessage ?? wrappedResponse?.errorMessage
                           ?? (isSuccess ? "Success" : "Unknown error");
            var ciStatus = (byte)(isSuccess ? 1 : 2);

            if (isSuccess)
            {
                ci022Success++;
                _logger.LogInformation("LoanKey:{LoanKey} CI-022 success.", businessItem.LoanKey);
                _logWriter.Log("CreditRegistrationBusinessDetails.txt",
                    $"LoanKey:{businessItem.LoanKey} CI-022 success.");
            }
            else
            {
                ci022Error++;
                _logger.LogError("LoanKey:{LoanKey} CI-022 error. Response:{Response}",
                    businessItem.LoanKey, response);
                _logWriter.Log("CreditRegistrationBusinessDetails.txt",
                    $"LoanKey:{businessItem.LoanKey} CI-022 error. Response:{response}");
            }

            await _creditBureauReportRepository.UpsertCiStatusAsync(
                businessItem.LoanKey, 22, ciStatus, message, null, cancellationToken);
        }
        _logger.LogInformation(
            "CI-022 completed. Processed={Processed}, Success={Success}, Error={Error}",
            ci022Processed, ci022Success, ci022Error);
        // ── CI-023: Сведения о связанных субъектах (залогодатель / поручитель) ────────
        var subjectRequests = await _creditBureauReportRepository.GetCreditRegistrationSubjectRequestsAsync(cancellationToken);
        _logger.LogInformation("CI-023 queue loaded. Count={Count}", subjectRequests.Count);
        foreach (var subjectItem in subjectRequests)
        {
            ci023Processed++;
            var request = subjectItem.Request;

            if (request is null)
            {
                ci023Error++;
                _logger.LogWarning("CI-023 skipped due to null request. LoanKey={LoanKey}", subjectItem.LoanKey);
                await _creditBureauReportRepository.UpsertCiStatusAsync(
                    subjectItem.LoanKey, 23, 2, "CI-023 request is null", null, cancellationToken);
                continue;
            }

            request.PHead = _asokiReportApiOptions.PHead;
            request.PCode = _asokiReportApiOptions.PCode;
            request.PDate = string.IsNullOrWhiteSpace(request.PDate)
                ? FormatKatmDate(DateTimeOffset.Now)
                : request.PDate.Trim();

            var baseRequest = new BaseRequest<CreditRegistrationSubjectRequest>
            {
                Data = request,
                Security = _requestSecurity
            };

            var response = await _requestManagerService.SendPostRequest(
                _asokiApplicationApiOptions.HostAddress + _asokiApplicationApiOptions.CreditRegistrationSubjectUrl,
                baseRequest.ToJSON(),
                subjectItem.LoanKey,
                cancellationToken);

            if (string.IsNullOrWhiteSpace(response))
            {
                ci023Error++;
                _logger.LogError("CI-023 empty response. LoanKey={LoanKey}", subjectItem.LoanKey);
                await _creditBureauReportRepository.UpsertCiStatusAsync(
                    subjectItem.LoanKey, 23, 2, "CI-023 returned empty response", null, cancellationToken);
                continue;
            }

            var wrappedResponse = JsonConvert.DeserializeObject<BaseResponse<CreditRegistrationResponse>>(response);
            var baseResponse = wrappedResponse?.data ?? JsonConvert.DeserializeObject<CreditRegistrationResponse>(response);
            var isSuccess = baseResponse?.result is CreditBureauResultCodes.SUCCESS_00000
                                                  or CreditBureauResultCodes.SUCCESS_05000;
            var message = baseResponse?.resultMessage ?? wrappedResponse?.errorMessage
                           ?? (isSuccess ? "Success" : "Unknown error");
            var ciStatus = (byte)(isSuccess ? 1 : 2);

            if (isSuccess)
            {
                ci023Success++;
                _logger.LogInformation("LoanKey:{LoanKey} CI-023 success. PLoanSubject={PLoanSubject}",
                    subjectItem.LoanKey, request.PLoanSubject);
                _logWriter.Log("CreditRegistrationSubject.txt",
                    $"LoanKey:{subjectItem.LoanKey} CI-023 success. PLoanSubject:{request.PLoanSubject}");
            }
            else
            {
                ci023Error++;
                _logger.LogError("LoanKey:{LoanKey} CI-023 error. Response:{Response}",
                    subjectItem.LoanKey, response);
                _logWriter.Log("CreditRegistrationSubject.txt",
                    $"LoanKey:{subjectItem.LoanKey} CI-023 error. Response:{response}");
            }

            await _creditBureauReportRepository.UpsertCiStatusAsync(
                subjectItem.LoanKey, 23, ciStatus, message, null, cancellationToken);
        }
        _logger.LogInformation(
            "CI-023 completed. Processed={Processed}, Success={Success}, Error={Error}",
            ci023Processed, ci023Success, ci023Error);

        processingStopwatch.Stop();
        _logger.LogInformation(
            "CreditBureauReportProcessing finished. ElapsedMs={ElapsedMs}",
            processingStopwatch.ElapsedMilliseconds);

    }
    private static string FormatKatmDate(DateTimeOffset value)
    {
        var formatted = value.ToString("yyyy-MM-dd'T'HH:mm:ss.fffzzz");
        return formatted.Remove(formatted.Length - 3, 1);
    }
}