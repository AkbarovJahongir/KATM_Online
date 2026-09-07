using Application.Repositories.CreditBureauRepositories;
using Infrastructure.Common.Helpers.Logger;
using Infrastructure.CreditRegistration;
using Infrastructure.CreditReports;

namespace Infrastructure.Services.CreditBureauServices
{
    public class LoanProcessingService(ICreditBureauRepository repository, ICreditRegistrationService creditRegistrationService, ICreditReportService creditReportService, LogWriter logWriter) : ICreditBureauService
    {
        private readonly ICreditBureauRepository _repository = repository;
        private readonly ICreditRegistrationService _creditRegistrationService = creditRegistrationService;
        private readonly ICreditReportService _creditReportService = creditReportService;
        private readonly LogWriter _logWriter = logWriter;
        public async Task CreditBureauProcessing(CancellationToken cancellationToken)
        {
            var loanApplications = await _repository.GetLoanApplications(cancellationToken);
            foreach (var application in loanApplications)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    if (application.Status == "00")
                    {
                        await _creditRegistrationService.SenderClaimsAsync(application, cancellationToken);
                        await _creditReportService.CreditReport(application, cancellationToken);
                    }
                    else if (application.Status is "02" or "03")
                    {
                        await _creditReportService.CreditReportStatus(application, cancellationToken);
                    }
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                {
                    // Одна упавшая заявка не должна останавливать обработку остальных в очереди.
                    _logWriter.Log(
                        "CreditBureauProcessingCatch.txt",
                        $"KeyCreditBureauKb: {application.KeyCreditBureauKb} ClaimId: {application.PClaimId} Status: {application.Status} - {DateTime.Now}\n\n{ex}");
                    continue;
                }
            }
        }
    }
}
