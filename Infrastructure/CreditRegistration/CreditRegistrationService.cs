using Application.CreditRegistration;
using Application.Repositories.Helpers;
using Application.Repositories.RequestManager;
using CreditBureau.Contracts.AsokiLoanApplications;
using CreditBureau.Contracts.AsokiLoanApplications.CreditRegistration.CreditApplications;
using Domain.Common.Constants;
using Domain.Common.Settings;
using Infrastructure.Common.Helpers.JsonHelpes;
using Infrastructure.Common.Helpers.Logger;
using Infrastructure.Services.HttpClients;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Infrastructure.CreditRegistration
{
    public class CreditRegistrationService(ICreditRegistrationRepository repository,
                                        IRequestManagerService requestManagerService,
                                        BankHeader bankHeader,
                                        LogWriter logWriter,
                                        AsokiApplicationApiOptions asokiApplicationApiOptions,
                                        IHelperRepository helperRepository,
                                        ILogger<CreditRegistrationService> logger) : ICreditRegistrationService
    {
        private readonly IHelperRepository _helperRepository = helperRepository;
        private readonly ICreditRegistrationRepository _repository = repository;
        private readonly IRequestManagerService _requestManagerService = requestManagerService;
        private readonly BankHeader _bankHeader = bankHeader;
        private readonly LogWriter _logWriter = logWriter;
        private readonly AsokiApplicationApiOptions _asokiApplicationApiOptions = asokiApplicationApiOptions;
        private readonly ILogger<CreditRegistrationService> _logger = logger;
        public async Task SenderClaimsAsync(LoanApplication loanApplications, CancellationToken cancellationToken)
        {
            _logWriter.Log("CreditRegistrationIndividual.txt", $"KeyAbsLoan:ClaimId:{loanApplications.PClaimId} - KeyRequestHistoryKb:{loanApplications.KeyLoanHistoryKb} START");
            if (loanApplications.ApplicationsSubjectType == "0")  // Физ. лицо
            {
                var creditRegistrationIndividual = await _repository.GetCreditRegistrationIndividualRequest(loanApplications.KeyLoanHistoryKb, cancellationToken);
                // если объект не найден
                if (creditRegistrationIndividual == null)
                {
                    _logger.LogError("Method in SenderClaimsAsync GetCreditRegistrationIndividualRequest вернул null!");
                    _logWriter.Log("CreditRegistrationIndividual.txt", "Method GetCreditRegistrationIndividualRequest вернул null!");
                    _logWriter.EmergencyLog("EmergencyLog.txt", $"KeyAbsLoan:ClaimId:{loanApplications.PClaimId} - KeyRequestHistoryKb:{loanApplications.KeyLoanHistoryKb}" + "Method GetCreditRegistrationIndividualRequest вернул null!");
                    return;
                }
                var baseRequestForCredit = new BaseRequestForCreditApplications<CreditRegistrationIndividualRequest>() { Header = _bankHeader, Request = creditRegistrationIndividual };
                try
                {
                    // Отправляем запрос
                    var response = await _requestManagerService.SendPostRequest(
                        _asokiApplicationApiOptions.HostAddress + _asokiApplicationApiOptions.IndividualPersonApplicationUrl,
                        baseRequestForCredit.ToJSON(),
                        loanApplications.KeyLoanHistoryKb,
                        IRequestManagerRepository.IsXml.NotXml,
                        cancellationToken);
                    if (string.IsNullOrWhiteSpace(response))
                        return;
                    var baseResponse = JsonConvert.DeserializeObject<CreditRegistrationSubjectResponse>(response);
                    // Успешно
                    if (baseResponse?.Result?.Code is CreditBureauResultCodes.SUCCESS_00000 or CreditBureauResultCodes.SUCCESS_05000)
                    {
                        _logger.LogInformation(message: $"KeyAbsLoan:ClaimId:{loanApplications.PClaimId} - KeyRequestHistoryKb:{loanApplications.KeyLoanHistoryKb} Update Table Response value:\\t" + baseResponse?.Response?.KatmSir);
                        _logWriter.Log("CreditRegistrationIndividual.txt", $"KeyAbsLoan:ClaimId:{loanApplications.PClaimId} - KeyRequestHistoryKb:{loanApplications.KeyLoanHistoryKb} Update Table Response value:\\t" + baseResponse?.Response?.KatmSir);
                        await _helperRepository.KatmHelper(loanApplications.KeyLoanHistoryKb, baseResponse?.Response?.KatmSir ?? "_", IHelperRepository.TypeOperation.IndividualRequestSuccessful, cancellationToken);
                    }
                    else
                    {
                        _logger.LogError("Error: {0}", response);
                        _logWriter.Log("CreditRegistrationIndividual.txt", string.Format("Error: {0}", response));
                        await _helperRepository.KatmHelper(loanApplications.KeyLoanHistoryKb, baseResponse?.Result?.Message, IHelperRepository.TypeOperation.Error, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logWriter.Log("CreditRegistrationIndividual.txt", "This Error In Catch Messages: " + ex.Message);
                    return;
                }
            }
            else // Юр лицо
            {
                //TODO
            }
            _logWriter.Log("CreditRegistrationIndividual.txt", $" END END\t");
        }

        public async Task SenderClaimsXmlAsync(LoanApplication loanApplications, CancellationToken cancellationToken)
        {
            _logWriter.Log("CreditRegistrationIndividualXml.txt", $"KeyAbsLoan:ClaimId:{loanApplications.PClaimId} - KeyRequestHistoryKb:{loanApplications.KeyLoanHistoryKb} START");
            if (loanApplications.ApplicationsSubjectType == "0")  // Физ. лицо
            {
                var creditRegistrationIndividual = await _repository.GetCreditRegistrationIndividualRequest(loanApplications.KeyLoanHistoryKb, cancellationToken);
                // если объект не найден
                if (creditRegistrationIndividual == null)
                {
                    _logger.LogError("Method in SenderClaimsAsync GetCreditRegistrationIndividualRequest вернул null!");
                    _logWriter.Log("CreditRegistrationIndividualXml.txt", "Method GetCreditRegistrationIndividualRequest вернул null!");
                    _logWriter.EmergencyLog("EmergencyLog.txt", $"KeyAbsLoan:ClaimId:{loanApplications.PClaimId} - KeyRequestHistoryKb:{loanApplications.KeyLoanHistoryKb}" + "Method GetCreditRegistrationIndividualRequest вернул null!");
                    return;
                }
                var baseRequestForCredit = new BaseRequestForCreditApplications<CreditRegistrationIndividualRequest>() { Header = _bankHeader, Request = creditRegistrationIndividual };
                try
                {
                    // Отправляем запрос
                    var response = await _requestManagerService.SendPostRequest(
                        _asokiApplicationApiOptions.HostAddress + _asokiApplicationApiOptions.IndividualPersonApplicationUrl,
                        baseRequestForCredit.ToJSON(),
                        loanApplications.KeyLoanHistoryKb,
                        IRequestManagerRepository.IsXml.Xml,
                        cancellationToken);
                    if (string.IsNullOrWhiteSpace(response))
                        return;
                    var baseResponse = JsonConvert.DeserializeObject<CreditRegistrationSubjectResponse>(response);
                    // Успешно
                    if (baseResponse?.Result?.Code is CreditBureauResultCodes.SUCCESS_00000 or CreditBureauResultCodes.SUCCESS_05000)
                    {
                        _logger.LogInformation(message: $"KeyAbsLoan:ClaimId:{loanApplications.PClaimId} - KeyRequestHistoryKb:{loanApplications.KeyLoanHistoryKb} Update Table Response value:\\t" + baseResponse?.Response?.KatmSir);
                        _logWriter.Log("CreditRegistrationIndividualXml.txt", $"KeyAbsLoan:ClaimId:{loanApplications.PClaimId} - KeyRequestHistoryKb:{loanApplications.KeyLoanHistoryKb} Update Table Response value:\\t" + baseResponse?.Response?.KatmSir);
                        await _helperRepository.KatmHelperXml(loanApplications.KeyLoanHistoryKb, baseResponse?.Response?.KatmSir ?? "_", IHelperRepository.TypeOperation.IndividualRequestSuccessful, cancellationToken);
                        _logWriter.Log("CreditRegistrationIndividualXml.txt", $"KeyAbsLoan:ClaimId:{loanApplications.PClaimId} - KeyRequestHistoryKb:{loanApplications.KeyLoanHistoryKb} Success Response value:\\t" + baseResponse?.Response?.KatmSir);
                    }
                    else
                    {
                        _logger.LogError("Error: {0}", response);
                        _logWriter.Log("CreditRegistrationIndividualXml.txt", string.Format("Error: {0}", response));
                        await _helperRepository.KatmHelperXml(loanApplications.KeyLoanHistoryKb, baseResponse?.Result?.Message, IHelperRepository.TypeOperation.Error, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logWriter.Log("CreditRegistrationIndividualXml.txt", "This Error In Catch Messages: " + ex.Message);
                    return;
                }
            }
            else // Юр лицо
            {
                //TODO
            }
            _logWriter.Log("CreditRegistrationIndividual.txt", $" END END\t");
        }
    }
}
