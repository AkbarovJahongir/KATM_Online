using CreditBureau.Contracts.AsokiLoanApplications;

namespace Application.Repositories.AsokiRepositories
{
    public interface IAsokiRepository
    {
        Task<List<LoanApplication>> GetLoanApplications(CancellationToken cancellationToken);
        Task<List<LoanApplication>> GetLoanApplicationsXml(CancellationToken cancellationToken);

    }
}
