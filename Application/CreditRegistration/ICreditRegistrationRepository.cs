using CreditBureauService.Contracts.CreditBureauApplications.CreditRegistration.CreditApplications;

namespace Application.CreditRegistration
{
    public interface ICreditRegistrationRepository
    {
        Task<CreditRegistrationIndividualRequest?> GetCreditRegistrationIndividualRequest(string keyAbsLoan, CancellationToken cancellationToken);
        Task<CreditRegistrationEntityRequest?> GetCreditRegistrationEntityRequest(string keyAbsLoan, CancellationToken cancellationToken);
    }
}
