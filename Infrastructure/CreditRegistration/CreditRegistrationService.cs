using Application.CreditRegistration;
using Application.Repositories.Helpers;
using Application.Repositories.RequestManager;
using Application.Repositories.CreditBureauRepositories;
using Application.Repositories.CreditBureauReportRepositories;
using CreditBureauService.Contracts.CreditBureauApplications;
using CreditBureauService.Contracts.CreditBureauApplications.CreditRegistration.CreditApplications;
using Domain.Common.Constants;
using Domain.Common.Settings;
using Infrastructure.Common.Helpers.JsonHelpes;
using Infrastructure.Common.Helpers.Logger;
using Infrastructure.Services.HttpClients;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Infrastructure.CreditRegistration
{
    public class CreditRegistrationService(
        ICreditRegistrationRepository repository,
        IRequestManagerService requestManagerService,
        BankHeader bankHeader,
        LogWriter logWriter,
        CreditBureauApiOptions creditBureauApiOptions,
        IHelperRepository helperRepository,
        ICreditBureauRepository creditBureauRepository,
        ICreditBureauReportRepository creditBureauReportRepository,
        ILogger<CreditRegistrationService> logger) : ICreditRegistrationService
    {
        private readonly IHelperRepository _helperRepository = helperRepository;
        private readonly ICreditRegistrationRepository _repository = repository;
        private readonly IRequestManagerService _requestManagerService = requestManagerService;
        private readonly BankHeader _bankHeader = bankHeader;
        private readonly LogWriter _logWriter = logWriter;
        private readonly CreditBureauApiOptions _creditBureauApiOptions = creditBureauApiOptions;
        private readonly ICreditBureauRepository _creditBureauRepository = creditBureauRepository;
        private readonly ICreditBureauReportRepository _creditBureauReportRepository = creditBureauReportRepository;
        private readonly ILogger<CreditRegistrationService> _logger = logger;

        public async Task SenderClaimsAsync(LoanApplication loanApplications, CancellationToken cancellationToken)
        {
            _logWriter.Log("CreditRegistrationIndividual.txt",
                $"KeyAbsLoan:ClaimId:{loanApplications.PClaimId} - KeyRequestHistoryKb:{loanApplications.KeyCreditBureauKb} START");
            if (loanApplications.ApplicationsSubjectType == "0") // Физ. лицо
            {
                if (int.TryParse(loanApplications.KeyCreditBureauKb, out int loanKey))
                {
                    await _creditBureauRepository.UpdateRequestHistoryXmlStatusAsync(
                        loanApplications.KeyCreditBureauKb, "02", cancellationToken);
                }
            }
            else
            {
                if (int.TryParse(loanApplications.KeyCreditBureauKb, out int loanKey))
                {
                    await _creditBureauRepository.UpdateRequestHistoryXmlStatusAsync(
                        loanApplications.KeyCreditBureauKb, "02", cancellationToken);
                }
            }

            _logWriter.Log("CreditRegistrationIndividual.txt", $" END END\t");
        }

        public async Task SenderClaimsXmlAsync(LoanApplication loanApplications, CancellationToken cancellationToken)
        {
            _logWriter.Log("CreditRegistrationIndividualXml.txt",
                $"KeyAbsLoan:ClaimId:{loanApplications.PClaimId} - KeyRequestHistoryKb:{loanApplications.KeyCreditBureauKb} START");
            if (loanApplications.ApplicationsSubjectType == "0") // Физ. лицо
            {
                await _creditBureauRepository.UpdateRequestHistoryXmlStatusAsync(loanApplications.KeyCreditBureauKb,
                    "02", cancellationToken);
            }
            else
            {
                await _creditBureauRepository.UpdateRequestHistoryXmlStatusAsync(loanApplications.KeyCreditBureauKb,
                    "02", cancellationToken);
            }
            _logWriter.Log("CreditRegistrationIndividual.txt", $" END END\t");
        }
    }
}