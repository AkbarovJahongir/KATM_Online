using Application.Repositories.AsokiRepositories;
using Infrastructure.CreditRegistration;
using Infrastructure.CreditReportsXml;

namespace Infrastructure.Services.AsokiXmlServices
{
    public class AsokiXmlService(IAsokiRepository repository, ICreditRegistrationService creditRegistrationService, ICreditReportXmlService creditReportService) : IAsokiXmlService
    {
        private readonly IAsokiRepository _repository = repository;
        private readonly ICreditReportXmlService _creditReportService = creditReportService;
        private readonly ICreditRegistrationService _creditRegistrationService = creditRegistrationService;
        public async Task AsokiProcessingXml(CancellationToken cancellationToken)
        {
            // получаем список
            var loanApplications = await _repository.GetLoanApplicationsXml(cancellationToken);
            foreach (var application in loanApplications)
            {
                if (application.Status is "00" or "01") // 00 (Заявка на отправку) / 01(входе отправки)
                {
                    await _creditRegistrationService.SenderClaimsXmlAsync(application, cancellationToken);
                }
                if (application.Status == "02") // 02 (Заявка на проверку истории)
                {
                    await _creditReportService.CreditReportXml(application, cancellationToken);
                }
                else if (application.Status == "03") // 03 (В обработке)
                {
                    await _creditReportService.CreditReportStatusXml(application, cancellationToken);
                }
            }
        }
    }
}
