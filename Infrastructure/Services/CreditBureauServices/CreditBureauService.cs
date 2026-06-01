using Application.Repositories.CreditBureauRepositories;
using Infrastructure.CreditRegistration;
using Infrastructure.CreditReports;

namespace Infrastructure.Services.CreditBureauServices
{
    public class LoanProcessingService(ICreditBureauRepository repository, ICreditRegistrationService creditRegistrationService, ICreditReportService creditReportService) : ICreditBureauService
    {
        private readonly ICreditBureauRepository _repository = repository;
        private readonly ICreditRegistrationService _creditRegistrationService = creditRegistrationService;
        private readonly ICreditReportService _creditReportService = creditReportService;
        public async Task CreditBureauProcessing(CancellationToken cancellationToken)
        {
            var loanApplications = await _repository.GetLoanApplications(cancellationToken);
            foreach (var application in loanApplications)
            {
                if (application.Status is "00" or "01")
                {
                    await _creditRegistrationService.SenderClaimsAsync(application, cancellationToken);
                    await _creditReportService.CreditReport(application, cancellationToken);
                }
                else if (application.Status == "02")
                {
                    await _creditReportService.CreditReport(application, cancellationToken);
                }
                else if (application.Status == "03")
                {
                    await _creditReportService.CreditReportStatus(application, cancellationToken);
                }
            }
        }
    }
}
