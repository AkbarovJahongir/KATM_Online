-- ============================================================================
-- SQL функции для периодической отправки отчетов CI-015, CI-016, CI-018 по периоду
-- ============================================================================

-- ============================================================================
-- KATM_Report_015_ByPeriod - Сведения об остатках на счетах за указанный период
-- ============================================================================
CREATE OR ALTER FUNCTION [dbo].[KATM_Report_015_ByPeriod]
(
    @StartDate DATE,
    @EndDate DATE
)
RETURNS TABLE
AS
RETURN
(
    SELECT
        lhk.keyLoanHistoryKb AS loanKey,
        -- Стандартные поля запроса
        'KATM' AS pHead,
        'KATM' AS pCode,
        la.App_old AS pContractId,
        la.S_ASOKI_0A6 AS pContractType,
        FORMAT(GETDATE(), 'dd.MM.yyyy HH:mm:ss') AS pDate,
        la.S_ASOKI_025 AS pLoanStatus,
        -- Поля счета
        a.Account AS account,
        FORMAT(ld.Date, 'dd.MM.yyyy HH:mm:ss') AS date,
        ISNULL(ld.StartBalance, 0) AS startBalance,
        ISNULL(ld.Debit, 0) AS debit,
        ISNULL(ld.Credit, 0) AS credit,
        ISNULL(ld.EndBalance, 0) AS endBalance
    FROM Loan_History_KB lhk
    INNER JOIN Loan_App la ON la.keyLoanHistoryKb = lhk.keyLoanHistoryKb
    INNER JOIN Account a ON a.keyLoanHistoryKb = lhk.keyLoanHistoryKb
    LEFT JOIN Loan_Details ld ON ld.keyAccount = a.keyAccount
        AND CAST(ld.Date AS DATE) BETWEEN @StartDate AND @EndDate
    WHERE
        lhk.ci004 = 1  -- CI-004 отправлен успешно
        AND (lhk.ci015 IS NULL OR lhk.ci015 = 0)  -- CI-015 ещё не отправлен
        AND CAST(ld.Date AS DATE) BETWEEN @StartDate AND @EndDate
)
GO

-- ============================================================================
-- KATM_Report_016_ByPeriod - Сведения о платежных документах за указанный период
-- ============================================================================
CREATE OR ALTER FUNCTION [dbo].[KATM_Report_016_ByPeriod]
(
    @StartDate DATE,
    @EndDate DATE
)
RETURNS TABLE
AS
RETURN
(
    SELECT
        lhk.keyLoanHistoryKb AS Lkey,
        -- Стандартные поля запроса
        la.Send_code AS Send_code,
        la.S_ASOKI_0A6 AS S_ASOKI_0A6,
        la.App_old AS App_old,
        -- Поля платежного документа
        pt.Account_db AS Account_db,
        pt.Account_kr AS Account_kr,
        pt.Send_code AS Send_code,
        pt.Reciver_code AS Reciver_code,
        pt.Curr_db AS Curr_db,
        pt.CB_60 AS CB_60,
        pt.Date_trans AS Date_trans,
        pt.N_doc AS N_doc,
        pt.UZ_s026 AS UZ_s026,
        pt.Send_name AS Send_name,
        pt.Reciver_name AS Reciver_name,
        pt.UZ_s004 AS UZ_s004,
        pt.transKey AS transKey,
        pt.Purpose AS Purpose,
        pt.Sum_NV_db AS Sum_NV_db
    FROM Loan_History_KB lhk
    INNER JOIN Loan_App la ON la.keyLoanHistoryKb = lhk.keyLoanHistoryKb
    INNER JOIN Payment_Trans pt ON pt.keyLoanHistoryKb = lhk.keyLoanHistoryKb
    WHERE
        lhk.ci004 = 1  -- CI-004 отправлен успешно
        AND (lhk.ci016 IS NULL OR lhk.ci016 = 0)  -- CI-016 ещё не отправлен
        AND CAST(pt.Date_trans AS DATE) BETWEEN @StartDate AND @EndDate
)
GO

-- ============================================================================
-- KATM_Report_018_ByPeriod - Сведения о статусе счетов за указанный период
-- ============================================================================
CREATE OR ALTER FUNCTION [dbo].[KATM_Report_018_ByPeriod]
(
    @StartDate DATE,
    @EndDate DATE
)
RETURNS TABLE
AS
RETURN
(
    SELECT
        lhk.keyLoanHistoryKb AS loanKey,
        -- Стандартные поля запроса
        la.App_old AS App_old,
        la.S_ASOKI_0A6 AS S_ASOKI_0A6,
        -- Поля статуса счета
        a.Account AS Account,
        a.S_ASOKI_025 AS AccountStatus,
        a.DateOpen AS DateOpen,
        a.DateClose AS DateClose,
        a.Balance AS Balance,
        a.OverdueAmount AS OverdueAmount,
        a.LastUpdateDate AS LastUpdateDate
    FROM Loan_History_KB lhk
    INNER JOIN Loan_App la ON la.keyLoanHistoryKb = lhk.keyLoanHistoryKb
    INNER JOIN Account a ON a.keyLoanHistoryKb = lhk.keyLoanHistoryKb
    WHERE
        lhk.ci004 = 1  -- CI-004 отправлен успешно
        AND (lhk.ci018 IS NULL OR lhk.ci018 = 0)  -- CI-018 ещё не отправлен
        AND CAST(a.LastUpdateDate AS DATE) BETWEEN @StartDate AND @EndDate
)
GO

-- ============================================================================
-- ПРИМЕЧАНИЕ ПО ИСПОЛЬЗОВАНИЮ:
-- ============================================================================
-- Эти функции предназначены для использования в методах репозитория:
-- - GetCreditRegistrationRepaymentRequestsByPeriodAsync
-- - GetCreditRegistrationBankDetailsRequestsByPeriodAsync
-- - GetAccountStatusRequestsByPeriodAsync
--
-- Пример вызова:
-- SELECT * FROM [dbo].[KATM_Report_015_ByPeriod]('2024-01-01', '2024-01-31')
-- SELECT * FROM [dbo].[KATM_Report_016_ByPeriod]('2024-01-01', '2024-01-31')
-- SELECT * FROM [dbo].[KATM_Report_018_ByPeriod]('2024-01-01', '2024-01-31')
-- ============================================================================
