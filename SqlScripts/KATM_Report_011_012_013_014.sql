-- ============================================================================
-- SQL функции для отчетов CI-011, CI-012, CI-013, CI-014
-- ВАЖНО:
-- 1. Скрипт согласован с колонками, которые ожидает CreditBureauReportRepository.
-- 2. Для CI-013/CI-014 могут потребоваться уточнения под фактическую схему АБС.
-- 3. Функции 011/012 опираются на уже существующие сущности и KATM_Report_005.
-- ============================================================================

-- ============================================================================
-- KATM_Report_011 - сведения о лизинговых договорах
-- Договор лизинга определяется по коду типа договора 0A6 = 2.
-- ============================================================================
CREATE OR ALTER FUNCTION [dbo].[KATM_Report_011]()
RETURNS TABLE
AS
RETURN
(
    SELECT
        lhk.loanKey AS loanKey,
        'KATM' AS pHead,
        'KATM' AS pCode,
        CAST(ISNULL(la.App_old, lhk.loanKey) AS varchar(50)) AS pClaimId,
        CAST(ISNULL(la.App_old, lhk.loanKey) AS varchar(50)) AS pContractId,
        CAST(la.N_National AS varchar(50)) AS pInn,
        CAST(la.ID AS varchar(50)) AS pNibbd,
        CASE
            WHEN la.Date_in2 IS NULL THEN NULL
            ELSE FORMAT(la.Date_in2, 'yyyy-MM-ddTHH:mm:ss.fff+0500')
        END AS pStartDate,
        NULL AS pEndDate,
        NULL AS pNotariusCertNumber,
        NULL AS pNotariusCertDate,
        NULL AS pNotariusRegNumber,
        NULL AS pNotariusRegDate,
        NULL AS pGovernmentRegNum,
        NULL AS pGovernmentRegDate,
        ISNULL(TRY_CONVERT(decimal(18, 2), la.Summ), 0) AS pCreditAmount,
        CAST(la.Curr AS varchar(10)) AS pCurrency,
        ISNULL(TRY_CONVERT(decimal(18, 2), la.Procent), 0) AS pPercent,
        ISNULL(coll.CollateralCount, 0) AS pCountObject,
        FORMAT(GETDATE(), 'yyyy-MM-ddTHH:mm:ss.fff+0500') AS pDate
    FROM CIB..Katm_Methods_Request lhk
    INNER JOIN Loan la ON la.keyLoanHistoryKb = lhk.loanKey
    OUTER APPLY
    (
        SELECT COUNT(1) AS CollateralCount
        FROM Loan_collateral lc
        WHERE lc.keyLoanHistoryKb = lhk.loanKey
    ) coll
    WHERE
        (lhk.ci001 = 1 OR lhk.ci002 = 1)
        AND (lhk.ci011 IS NULL OR lhk.ci011 IN (0, 2))
);
GO

-- ============================================================================
-- KATM_Report_012 - график погашения лизингового договора
-- Использует существующую функцию KATM_Report_005 и отбирает только лизинг.
-- ============================================================================
CREATE OR ALTER FUNCTION [dbo].[KATM_Report_012]()
RETURNS TABLE
AS
RETURN
(
    SELECT
        src.loanKey,
        'KATM' AS pHead,
        'KATM' AS pCode,
        src.pClaimId,
        src.pContractId,
        src.PNibbd AS pNibbd,
        FORMAT(GETDATE(), 'yyyy-MM-ddTHH:mm:ss.fff+0500') AS pDate,
        '0' AS pIsUpdate,
        src.[Date] AS scheduleDate,
        src.[Percent] AS schedulePercent,
        src.[Currency] AS scheduleCurrency,
        src.[Amount] AS scheduleAmount
    FROM [dbo].[KATM_Report_005]() src
    INNER JOIN CIB..Katm_Methods_Request lhk ON lhk.loanKey = src.loanKey
    INNER JOIN Loan la ON la.keyLoanHistoryKb = lhk.loanKey
    WHERE
        lhk.ci011 = 1
        AND (lhk.ci012 IS NULL OR lhk.ci012 IN (0, 2))
);
GO

-- ============================================================================
-- KATM_Report_013 - сведения об объектах лизингового договора
-- Основано на существующей функции KATM_Report_021 (обеспечение) с алиасами,
-- которые ожидает CreditBureauReportRepository.
-- При необходимости замените источник на специализированную таблицу по лизингу.
-- ============================================================================
CREATE OR ALTER FUNCTION [dbo].[KATM_Report_013]()
RETURNS TABLE
AS
RETURN
(
    SELECT
        lhk.loanKey AS loanKey,
        CAST(ISNULL(la.App_old, lhk.loanKey) AS varchar(50)) AS pContractId,
        src.pOwnerId AS pClientId,
        CAST(la.N_National AS varchar(50)) AS pInn,
        CAST(la.ID AS varchar(50)) AS pNibbd,
        FORMAT(GETDATE(), 'yyyy-MM-ddTHH:mm:ss.fff+0500') AS pDate,
        src.pGuaranteeId AS detailObjectId,
        src.pSumma AS detailAmount,
        src.pCurrency AS detailCurrency,
        src.pGuaranteeType AS detailLeasingType,
        src.pName AS detailName,
        ISNULL(src.pStatus, '001') AS detailStatus,
        NULL AS detailAmortization,
        FORMAT(GETDATE(), 'yyyy-MM-ddTHH:mm:ss.fff+0500') AS detailDate,
        src.pDescription AS detailDetails
    FROM [dbo].[KATM_Report_021]() src
    INNER JOIN CIB..Katm_Methods_Request lhk ON lhk.loanKey = src.loanKey
    INNER JOIN Loan la ON la.keyLoanHistoryKb = lhk.loanKey
    WHERE
        lhk.ci011 = 1
        AND (lhk.ci013 IS NULL OR lhk.ci013 IN (0, 2))
);
GO

-- ============================================================================
-- KATM_Report_014 - сведения о договорах факторинга
-- В текущем виде отбирает договоры, прошедшие CI-001/CI-002 и еще не отправленные
-- в CI-014. Если в вашей АБС есть отдельный признак факторинга, добавьте его в WHERE.
-- ============================================================================
CREATE OR ALTER FUNCTION [dbo].[KATM_Report_014]()
RETURNS TABLE
AS
RETURN
(
    SELECT
        lhk.loanKey AS loanKey,
        'KATM' AS pHead,
        'KATM' AS pCode,
        CAST(ISNULL(la.App_old, lhk.loanKey) AS varchar(50)) AS pClaimId,
        CAST(ISNULL(la.App_old, lhk.loanKey) AS varchar(50)) AS pContractId,
        CAST(la.N_National AS varchar(50)) AS pInn,
        CAST(la.ID AS varchar(50)) AS pNibbd,
        ISNULL(TRY_CONVERT(decimal(18, 2), la.Summ), 0) AS pCreditAmount,
        CAST(la.Curr AS varchar(10)) AS pCurrency,
        NULL AS pBankElement,
        NULL AS pFactoringNumber,
        NULL AS pSummaLiability,
        NULL AS pSummaDiscount,
        NULL AS pInnDebtor,
        FORMAT(GETDATE(), 'yyyy-MM-ddTHH:mm:ss.fff+0500') AS pDate,
        CASE
            WHEN la.Date_in2 IS NULL THEN NULL
            ELSE FORMAT(la.Date_in2, 'yyyy-MM-ddTHH:mm:ss.fff+0500')
        END AS pStartDate,
        NULL AS pEndDate
    FROM CIB..Katm_Methods_Request lhk
    INNER JOIN Loan la ON la.keyLoanHistoryKb = lhk.loanKey
    WHERE
        (lhk.ci001 = 1 OR lhk.ci002 = 1)
        AND (lhk.ci014 IS NULL OR lhk.ci014 IN (0, 2))
);
GO
