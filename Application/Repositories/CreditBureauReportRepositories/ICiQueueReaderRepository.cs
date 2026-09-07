using CreditBureauService.Contracts.CreditBureauApplications.CreditRegistration.CreditAgreementsAndLeasing.Requests;
using CreditBureauService.Contracts.CreditBureauApplications.CreditRegistration.CreditApplications;

namespace Application.Repositories.CreditBureauReportRepositories;

/// <summary>
/// Queue readers for CI-001..023 TVF / period loads.
/// </summary>
public interface ICiQueueReaderRepository
{
    Task<List<CreditBureauReportQueueItem<CreditRegistrationIndividualRequest>>>
        GetCreditRegistrationIndividualRequestsAsync(CancellationToken cancellationToken);

    Task<List<CreditBureauReportQueueItem<CreditRegistrationEntityRequest>>>
        GetCreditRegistrationEntityRequestsAsync(CancellationToken cancellationToken);

    Task<List<CreditBureauReportQueueItem<CreditRegistrationDeclineRequest>>>
        GetCreditRegistrationDeclineRequestsAsync(CancellationToken cancellationToken);

    Task<List<CreditBureauReportQueueItem<CreditRegistrationRequest>>> GetCreditRegistrationRequestsAsync(
        CancellationToken cancellationToken);

    Task<List<CreditBureauReportQueueItem<CreditRegistrationRepaymentSchedule>>>
        CreditRegistrationRepaymentSchedulesAsync(CancellationToken cancellationToken);

    Task<List<CreditBureauReportQueueItem<CreditRegistrationPledgeOwner>>> CreditRegistrationPledgeOwnerAsync(
        CancellationToken cancellationToken);

    Task<List<CreditBureauReportQueueItem<CreditRegistrationPledgeSecurity>>> GetPledgeSecurityRequestsAsync(
        CancellationToken cancellationToken);

    Task<List<CreditBureauReportQueueItem<CreditRegistrationRepayment>>>
        GetCreditRegistrationRepaymentRequestsAsync(CancellationToken cancellationToken);

    Task<List<CreditBureauReportQueueItem<CreditRegistrationRepayment>>>
        GetCreditRegistrationRepaymentRequestsByDateAsync(DateTime date, CancellationToken cancellationToken);

    Task<List<CreditBureauReportQueueItem<CreditRegistrationBankDitailRequest>>>
        GetCreditRegistrationBankDetailsRequestsAsync(CancellationToken cancellationToken);

    Task<List<CreditBureauReportQueueItem<CreditRegistrationBankDitailRequest>>>
        GetCreditRegistrationBankDetailsRequestsByDateAsync(DateTime date, CancellationToken cancellationToken);

    Task<List<CreditBureauReportQueueItem<CreditRegistrationAccountStatus>>>
        GetAccountStatusRequestsAsync(CancellationToken cancellationToken);

    Task<List<CreditBureauReportQueueItem<CreditRegistrationAccountStatus>>>
        GetAccountStatusRequestsByDateAsync(DateTime date, CancellationToken cancellationToken);

    Task<List<CreditBureauReportQueueItem<CreditRegistrationRepayment>>>
        GetCreditRegistrationRepaymentRequestsByPeriodAsync(DateTime startDate, DateTime endDate,
            CancellationToken cancellationToken);

    Task<List<CreditBureauReportQueueItem<CreditRegistrationBankDitailRequest>>>
        GetCreditRegistrationBankDetailsRequestsByPeriodAsync(DateTime startDate, DateTime endDate,
            CancellationToken cancellationToken);

    Task<List<CreditBureauReportQueueItem<CreditRegistrationAccountStatus>>>
        GetAccountStatusRequestsByPeriodAsync(DateTime startDate, DateTime endDate,
            CancellationToken cancellationToken);

    Task<List<CreditBureauReportQueueItem<CreditRegistrationRepayment>>>
        GetCreditRegistrationRepaymentRequestsByPeriodAsync(DateTime startDate, DateTime endDate, int? loanKey,
            CancellationToken cancellationToken);

    Task<List<CreditBureauReportQueueItem<CreditRegistrationBankDitailRequest>>>
        GetCreditRegistrationBankDetailsRequestsByPeriodAsync(DateTime startDate, DateTime endDate, int? loanKey,
            CancellationToken cancellationToken);

    Task<List<CreditBureauReportQueueItem<CreditRegistrationAccountStatus>>>
        GetAccountStatusRequestsByPeriodAsync(DateTime startDate, DateTime endDate, int? loanKey,
            CancellationToken cancellationToken);

    Task<List<CreditBureauReportQueueItem<CreditRegistrationLeasingRequest>>>
        GetCreditRegistrationLeasingRequestsAsync(CancellationToken cancellationToken);

    Task<List<CreditBureauReportQueueItem<CreditRegistrationLeasingRepaymentSchedule>>>
        GetLeasingRepaymentSchedulesAsync(CancellationToken cancellationToken);

    Task<List<CreditBureauReportQueueItem<CreditRegistrationLeasingRepayment>>>
        GetLeasingRepaymentObjectsAsync(CancellationToken cancellationToken);

    Task<List<CreditBureauReportQueueItem<CreditRegistrationFactoring>>>
        GetCreditRegistrationFactoringRequestsAsync(CancellationToken cancellationToken);

    Task<List<CreditBureauReportQueueItem<CreditRegistrationBusinessDetailRequest>>>
        GetCreditRegistrationBusinessDetailsRequestsAsync(CancellationToken cancellationToken);

    Task<List<CreditBureauReportQueueItem<CreditRegistrationSubjectRequest>>>
        GetCreditRegistrationSubjectRequestsAsync(CancellationToken cancellationToken);
}
