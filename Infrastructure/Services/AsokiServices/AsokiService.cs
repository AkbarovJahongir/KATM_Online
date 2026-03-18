using Application.Repositories.AsokiRepositories;
using Infrastructure.CreditRegistration;
using Infrastructure.CreditReports;

namespace Infrastructure.Services.AsokiServices
{
    public class AsokiService(IAsokiRepository repository, ICreditRegistrationService creditRegistrationService, ICreditReportService creditReportService) : IAsokiService
    {
        private readonly IAsokiRepository _repository = repository;
        private readonly ICreditRegistrationService _creditRegistrationService = creditRegistrationService;
        private readonly ICreditReportService _creditReportService = creditReportService;
        public async Task AsokiProcessing(CancellationToken cancellationToken)
        {
            // получаем список
            var loanApplications = await _repository.GetLoanApplications(cancellationToken);
            foreach (var application in loanApplications)
            {
                // разделяем по статусам
                if (application.Status is "00" or "01") // 00 (Заявка на отправку) / 01(входе отправки)
                {
                    await _creditRegistrationService.SenderClaimsAsync(application, cancellationToken);
                }
                else if (application.Status == "02") // 02 (Заявка на проверку истории)
                {
                    await _creditReportService.CreditReport(application, cancellationToken);
                }
                else if (application.Status == "03") // 03 (В обработке)
                {
                    await _creditReportService.CreditReportStatus(application, cancellationToken);
                }
            }
        }
    }
}
