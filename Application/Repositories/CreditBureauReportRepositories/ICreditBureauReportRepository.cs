using CreditBureau.Contracts.AsokiLoanApplications.CreditRegistration.CreditAgreementsAndLeasing.Requests;
using CreditBureau.Contracts.AsokiLoanApplications.CreditRegistration.CreditApplications;

namespace Application.Repositories.CreditBureauReportRepositories;

public interface ICreditBureauReportRepository
{
    /// <summary>
    /// �������� ��� ������ �� ����������� ������� (���.���), ������� ���� ��������� � �� � �� ���� ���������� � ��������� ���� 001
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<List<CreditBureauReportQueueItem<CreditRegistrationIndividualRequest>>>
        GetCreditRegistrationIndividualRequestsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// �������� ��� ������ �� ����������� ������� (��.���), ������� ���� ��������� � �� � �� ���� ���������� � ��������� ���� 002
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<List<CreditBureauReportQueueItem<CreditRegistrationEntityRequest>>>
        GetCreditRegistrationEntityRequestsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// �������� ��� ���������� ������ 003
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<List<CreditBureauReportQueueItem<CreditRegistrationDeclineRequest>>>
        GetCreditRegistrationDeclineRequestsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// �������� ��� c������� � ��������� �������� (CI-004) 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<List<CreditBureauReportQueueItem<CreditRegistrationRequest>>> GetCreditRegistrationRequestsAsync(
        CancellationToken cancellationToken);

    /// <summary>
    /// �������� ��� �������� � ������� ��������� (CI-005)
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<List<CreditBureauReportQueueItem<CreditRegistrationRepaymentSchedule>>>
        CreditRegistrationRepaymentSchedulesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// �������� ������ �������� �� ���������������� CI-020.
    /// ciStatus: 0 = �� ����������, 1 = �������, 2 = ������.
    /// </summary>
    public Task<List<CreditBureauReportQueueItem<CreditRegistrationPledgeOwner>>> CreditRegistrationPledgeOwnerAsync(
        CancellationToken cancellationToken);
    /// <summary>
    /// Получить все записи об обеспечении кредита (CI-021),
    /// для которых ci005 = 1, ci020 = 1 и ci021 ещё не отправлен.
    /// Источники: Loan_collateral (залог) и Loan_guarantor (поручительство).
    /// </summary>
    public Task<List<CreditBureauReportQueueItem<CreditRegistrationPledgeSecurity>>> GetPledgeSecurityRequestsAsync(CancellationToken cancellationToken);
    /// <summary>
    /// Получить все записи об остатках на счетах (CI-015),
    /// для которых ci004 = 1 и ci015 ещё не отправлен (ciStatus = 0).
    /// </summary>
    public Task<List<CreditBureauReportQueueItem<CreditRegistrationRepayment>>>
        GetCreditRegistrationRepaymentRequestsAsync(CancellationToken cancellationToken);
    /// <summary>
    /// Получить все записи об остатках на счетах (CI-016),
    /// для которых ci004 = 1 и ci016 ещё не отправлен (ciStatus = 0).
    /// </summary>
    public Task<List<CreditBureauReportQueueItem<CreditRegistrationBankDitailRequest>>>
        GetCreditRegistrationBankDetailsRequestsAsync(CancellationToken cancellationToken);
    /// <summary>
    /// Получить все записи о статусах счетов (CI-018),
    /// для которых ci004 = 1 и ci018 ещё не отправлен.
    /// Несколько счетов на один займ группируются в pAccountStatusesArray[].
    /// </summary>
    public Task<List<CreditBureauReportQueueItem<CreditRegistrationAccountStatus>>> 
        GetAccountStatusRequestsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Получить все лизинговые договоры (CI-011),
    /// для которых ci001 = 1 или ci002 = 1, и ci011 ещё не отправлен или вернул ошибку.
    /// </summary>
    public Task<List<CreditBureauReportQueueItem<CreditRegistrationLeasingRequest>>>
        GetCreditRegistrationLeasingRequestsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Получить график погашения лизинговых договоров (CI-012).
    /// Возвращает записи, у которых ci011 = 1 и ci012 ещё не отправлен.
    /// C# группирует строки по loanKey и формирует pPlanArray.
    /// </summary>
    public Task<List<CreditBureauReportQueueItem<CreditRegistrationLeasingRepaymentSchedule>>>
        GetLeasingRepaymentSchedulesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Получить сведения об объектах лизинговых договоров (CI-013).
    /// Возвращает записи, у которых ci011 = 1 и ci013 ещё не отправлен.
    /// Источник: Loan_collateral. C# группирует по loanKey и формирует pDetailsArray.
    /// </summary>
    public Task<List<CreditBureauReportQueueItem<CreditRegistrationLeasingRepayment>>>
        GetLeasingRepaymentObjectsAsync(CancellationToken cancellationToken);

    public Task UpsertCiStatusAsync(
        int loanKey,
        int ciCode,
        byte ciStatus,
        string? message,
        string? token,
        CancellationToken cancellationToken);
}