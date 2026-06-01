using Application.Repositories.CreditBureauRepositories;
using Infrastructure.CreditRegistration;
using Infrastructure.CreditReportsXml;

namespace Infrastructure.Services.CreditBureauXmlServices
{
    public class LoanProcessingXmlService(ICreditBureauRepository repository, ICreditRegistrationService creditRegistrationService, ICreditReportXmlService creditReportService) : ICreditBureauXmlService
    {
        private readonly ICreditBureauRepository _repository = repository;
        private readonly ICreditReportXmlService _creditReportService = creditReportService;
        private readonly ICreditRegistrationService _creditRegistrationService = creditRegistrationService;
        public async Task CreditBureauProcessingXml(CancellationToken cancellationToken)
        {
            var loanApplications = await _repository.GetLoanApplicationsXml(cancellationToken);
            foreach (var application in loanApplications)
            {
                if (application.Status is "00" or "01")
                {
                    await _creditRegistrationService.SenderClaimsXmlAsync(application, cancellationToken);
                    await _creditReportService.CreditReportXml(application, cancellationToken);
                }
                if (application.Status == "02")
                {
                    await _creditReportService.CreditReportXml(application, cancellationToken);
                }
                else if (application.Status == "03")
                {
                    await _creditReportService.CreditReportStatusXml(application, cancellationToken);
                }
            }
        }
    }
}
