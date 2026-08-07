using CreditBureauService.Contracts.CreditBureauApplications.CreditRegistration.CreditAgreementsAndLeasing.Requests;
using CreditBureauService.Contracts.CreditBureauApplications.CreditRegistration.CreditApplications;

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
    /// Получить все записи об остатках на счетах (CI-015) за указанную дату,
    /// для которых ci004 = 1 и ci015 ещё не отправлен (ciStatus = 0).
    /// </summary>
    /// <param name="date">Дата для выборки данных</param>
    /// <param name="cancellationToken">Токен отмены</param>
    public Task<List<CreditBureauReportQueueItem<CreditRegistrationRepayment>>>
        GetCreditRegistrationRepaymentRequestsByDateAsync(DateTime date, CancellationToken cancellationToken);

    /// <summary>
    /// Получить все записи об остатках на счетах (CI-016),
    /// для которых ci004 = 1 и ci016 ещё не отправлен (ciStatus = 0).
    /// </summary>
    public Task<List<CreditBureauReportQueueItem<CreditRegistrationBankDitailRequest>>>
        GetCreditRegistrationBankDetailsRequestsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Получить все записи о платежных документах (CI-016) за указанную дату,
    /// для которых ci004 = 1 и ci016 ещё не отправлен (ciStatus = 0).
    /// </summary>
    /// <param name="date">Дата для выборки данных</param>
    /// <param name="cancellationToken">Токен отмены</param>
    public Task<List<CreditBureauReportQueueItem<CreditRegistrationBankDitailRequest>>>
        GetCreditRegistrationBankDetailsRequestsByDateAsync(DateTime date, CancellationToken cancellationToken);

    /// <summary>
    /// Получить все записи о статусах счетов (CI-018),
    /// для которых ci004 = 1 и ci018 ещё не отправлен.
    /// Несколько счетов на один займ группируются в pAccountStatusesArray[].
    /// </summary>
    public Task<List<CreditBureauReportQueueItem<CreditRegistrationAccountStatus>>>
        GetAccountStatusRequestsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Получить все записи о статусах счетов (CI-018) за указанную дату,
    /// для которых ci004 = 1 и ci018 ещё не отправлен.
    /// Несколько счетов на один займ группируются в pAccountStatusesArray[].
    /// </summary>
    /// <param name="date">Дата для выборки данных</param>
    /// <param name="cancellationToken">Токен отмены</param>
    public Task<List<CreditBureauReportQueueItem<CreditRegistrationAccountStatus>>>
        GetAccountStatusRequestsByDateAsync(DateTime date, CancellationToken cancellationToken);

    /// <summary>
    /// Получить все записи об остатках на счетах (CI-015) за указанный период,
    /// для которых ci004 = 1 и ci015 ещё не отправлен (ciStatus = 0).
    /// </summary>
    /// <param name="startDate">Дата начала периода</param>
    /// <param name="endDate">Дата окончания периода</param>
    /// <param name="cancellationToken">Токен отмены</param>
    public Task<List<CreditBureauReportQueueItem<CreditRegistrationRepayment>>>
        GetCreditRegistrationRepaymentRequestsByPeriodAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken);

    /// <summary>
    /// Получить все записи о платежных документах (CI-016) за указанный период,
    /// для которых ci004 = 1 и ci016 ещё не отправлен (ciStatus = 0).
    /// </summary>
    /// <param name="startDate">Дата начала периода</param>
    /// <param name="endDate">Дата окончания периода</param>
    /// <param name="cancellationToken">Токен отмены</param>
    public Task<List<CreditBureauReportQueueItem<CreditRegistrationBankDitailRequest>>>
        GetCreditRegistrationBankDetailsRequestsByPeriodAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken);

    /// <summary>
    /// Получить все записи о статусах счетов (CI-018) за указанный период,
    /// для которых ci004 = 1 и ci018 ещё не отправлен.
    /// Несколько счетов на один займ группируются в pAccountStatusesArray[].
    /// </summary>
    /// <param name="startDate">Дата начала периода</param>
    /// <param name="endDate">Дата окончания периода</param>
    /// <param name="cancellationToken">Токен отмены</param>
    public Task<List<CreditBureauReportQueueItem<CreditRegistrationAccountStatus>>>
        GetAccountStatusRequestsByPeriodAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken);

    /// <summary>
    /// Получить все записи об остатках на счетах (CI-015) за указанный период,
    /// для которых ci004 = 1 и ci015 ещё не отправлен (ciStatus = 0).
    /// </summary>
    /// <param name="startDate">Дата начала периода</param>
    /// <param name="endDate">Дата окончания периода</param>
    /// <param name="loanKey">Фильтр по LoanKey (необязательно)</param>
    /// <param name="cancellationToken">Токен отмены</param>
    public Task<List<CreditBureauReportQueueItem<CreditRegistrationRepayment>>>
        GetCreditRegistrationRepaymentRequestsByPeriodAsync(DateTime startDate, DateTime endDate, int? loanKey, CancellationToken cancellationToken);

    /// <summary>
    /// Получить все записи о платежных документах (CI-016) за указанный период,
    /// для которых ci004 = 1 и ci016 ещё не отправлен (ciStatus = 0).
    /// </summary>
    /// <param name="startDate">Дата начала периода</param>
    /// <param name="endDate">Дата окончания периода</param>
    /// <param name="loanKey">Фильтр по LoanKey (необязательно)</param>
    /// <param name="cancellationToken">Токен отмены</param>
    public Task<List<CreditBureauReportQueueItem<CreditRegistrationBankDitailRequest>>>
        GetCreditRegistrationBankDetailsRequestsByPeriodAsync(DateTime startDate, DateTime endDate, int? loanKey, CancellationToken cancellationToken);

    /// <summary>
    /// Получить все записи о статусах счетов (CI-018) за указанный период,
    /// для которых ci004 = 1 и ci018 ещё не отправлен.
    /// Несколько счетов на один займ группируются в pAccountStatusesArray[].
    /// </summary>
    /// <param name="startDate">Дата начала периода</param>
    /// <param name="endDate">Дата окончания периода</param>
    /// <param name="loanKey">Фильтр по LoanKey (необязательно)</param>
    /// <param name="cancellationToken">Токен отмены</param>
    public Task<List<CreditBureauReportQueueItem<CreditRegistrationAccountStatus>>>
        GetAccountStatusRequestsByPeriodAsync(DateTime startDate, DateTime endDate, int? loanKey, CancellationToken cancellationToken);

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
    /// <summary>
    /// Получить все записи о договорах факторинга (CI-014),
    /// для которых ci001 = 1 или ci002 = 1, и ci014 ещё не отправлен.
    /// </summary>
    public Task<List<CreditBureauReportQueueItem<CreditRegistrationFactoring>>>
        GetCreditRegistrationFactoringRequestsAsync(CancellationToken cancellationToken);
    /// <summary>
    /// Получить все записи о бухгалтерских проводках для хозяйствующих субъектов (CI-022),
    /// для которых ci005 = 1 и ci022 ещё не отправлен.
    /// Источник: Trans (проводки за текущую банковскую дату, только юр.лица).
    /// C# группирует строки по loanKey и формирует pRepaymentDetArray.
    /// </summary>
    public Task<List<CreditBureauReportQueueItem<CreditRegistrationBusinessDetailRequest>>>
        GetCreditRegistrationBusinessDetailsRequestsAsync(CancellationToken cancellationToken);
    /// <summary>
    /// Получить все записи о связанных субъектах (CI-023):
    /// залогодателях (Loan_collateral) и поручителях (Loan_guarantor),
    /// для которых ci001 = 1 или ci002 = 1, и ci023 ещё не отправлен.
    /// Одна строка = один субъект (без группировки в C#).
    /// </summary>
    public Task<List<CreditBureauReportQueueItem<CreditRegistrationSubjectRequest>>>
        GetCreditRegistrationSubjectRequestsAsync(CancellationToken cancellationToken);
    /// <summary>
    /// Получить текущее состояние CI-017 для займа: число попыток, последний увиденный
    /// бизнес-статус заявки и время последней реальной попытки обращения к бюро.
    /// </summary>
    Task<Ci017State> GetCi017StateAsync(int loanKey, CancellationToken cancellationToken);

    /// <summary>
    /// Обнулить счётчик попыток CI-017 и зафиксировать новый бизнес-статус заявки.
    /// Вызывается, когда попытки уже исчерпаны и статус заявки с тех пор изменился —
    /// старые попытки больше не актуальны, обработку можно возобновить.
    /// </summary>
    Task ResetCi017AttemptAsync(int loanKey, string? newStatus, CancellationToken cancellationToken);

    /// <summary>
    /// Обновить бизнес-статус заявки в [dbo].[Request_History] (Key_ABS_Loan = loanKey).
    /// Используется, например, чтобы выставить статус "09" после исчерпания попыток CI-017.
    /// </summary>
    Task UpdateRequestHistoryStatusAsync(int loanKey, string status, CancellationToken cancellationToken);

    /// <summary>
    /// Увеличить счётчик попыток CI-017 и зафиксировать статус заявки, при котором
    /// была сделана эта попытка (используется для последующего сравнения в GetCi017StateAsync).
    /// </summary>
    Task IncrementCi017AttemptAsync(int loanKey, string? currentStatus, CancellationToken cancellationToken);

    /// <summary>
    /// Записать запрос/ответ CI-017 (отчёт или проверка статуса) в Ci017RequestLog.
    /// </summary>
    Task InsertCi017RequestLogAsync(
        int loanKey,
        string? claimId,
        string requestType,
        int attemptNumber,
        string? requestBody,
        string? responseBody,
        DateTime dateRequest,
        DateTime? dateResponse,
        CancellationToken cancellationToken);

    public Task UpsertCiStatusAsync(
        int loanKey,
        int ciCode,
        byte ciStatus,
        string? message,
        string? token,
        CancellationToken cancellationToken);

    /// <summary>
    /// Получить статус CreditBureau001 для конкретного займа (физ. лицо).
    /// </summary>
    /// <param name="loanKey">Ключ займа</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Статус CreditBureau001 (0 = не отправлен, 1 = отправлен)</returns>
    public Task<byte?> GetCreditBureau001StatusAsync(int loanKey, CancellationToken cancellationToken);

    /// <summary>
    /// Получить статус CreditBureau002 для конкретного займа (юр. лицо).
    /// </summary>
    /// <param name="loanKey">Ключ займа</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Статус CreditBureau002 (0 = не отправлен, 1 = отправлен)</returns>
    public Task<byte?> GetCreditBureau002StatusAsync(int loanKey, CancellationToken cancellationToken);

    /// <summary>
    /// Получить APP (App) и Customer_ID из таблицы Loan по LoanKey (через JOIN с CIB.dbo.Request_History по Key_Abs_Loan)
    /// </summary>
    Task<(string? App, string? CustomerId)> GetLoanAppAndCustomerIdAsync(string loanKey, CancellationToken cancellationToken);
}