/* Выполнять на том же экземпляре SQL Server, где находятся Ehtirom и CIB. */

USE [Ehtirom];
GO

ALTER PROCEDURE [dbo].[Loan_History_KB_Request]
(
    @kibType VARCHAR(30)
)
AS
BEGIN
    SET NOCOUNT ON;

    IF LOWER(@kibType) = 'katm'
    BEGIN
        SELECT TOP 10
            lhk.[key] AS [keyLoanHistoryKb],
            l.[key] + Opt.claim_offset AS pClaimId,
            rh.[Request_Subtype] AS pReportId,
            2 AS pReportReason,
            NULL AS pLoanSubject,
            NULL AS pLoanSubjectType,
            NULL AS pPin,
            NULL AS pTin,
            rh.Token AS pToken,
            rh.[Status] AS [Status],
            rh.[QuantitySelected] AS [QuantitySelected],
            c.[type] AS [ApplicationsSubjectType]
        FROM [CIB].[dbo].[Request_History] AS rh
        CROSS APPLY (SELECT [value] AS claim_offset FROM [dbo].[Options] WHERE ID = '287') AS Opt
        INNER JOIN [dbo].[Loan_History_KB] AS lhk
            ON lhk.[key] = rh.Key_Abs_Loan
           AND lhk.[Status] NOT IN ('03', '09')
           AND lhk.[Type] = 1
        INNER JOIN [dbo].[Loan] AS l
            ON l.App = lhk.[App]
           AND l.UAN IN ('02', '03', '04')
        INNER JOIN [dbo].[Customer] AS c
            ON c.Id = l.Customer_ID
        WHERE rh.Kib_Id = '06'
          AND rh.[Status] NOT IN ('04', '09')
          AND DATEDIFF(SECOND, rh.[Next_Access_Date], GETDATE()) >= 0
          AND NOT (
                rh.[Status] IN ('02', '03')
                AND NULLIF(LTRIM(RTRIM(rh.Token)), '') IS NULL
          );
    END;
END;
GO

USE [CIB];
GO

ALTER PROCEDURE [dbo].[Get_Request_History_Xml]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 10
        lhk.[key] AS [keyLoanHistoryKb],
        l.[key] + Opt.claim_offset AS pClaimId,
        rh.[Request_Subtype] AS pReportId,
        2 AS pReportReason,
        NULL AS pLoanSubject,
        NULL AS pLoanSubjectType,
        NULL AS pPin,
        NULL AS pTin,
        rh.Token AS pToken,
        rh.[Status] AS [Status],
        rh.[QuantitySelected] AS [QuantitySelected],
        c.[type] AS [ApplicationsSubjectType]
    FROM [dbo].[Request_History_Xml] AS rh
    CROSS APPLY (SELECT s.[value] AS claim_offset FROM [dbo].[Settings] AS s WHERE s.Id = '287') AS Opt
    INNER JOIN [Ehtirom].[dbo].[Loan_History_KB] AS lhk
        ON lhk.[key] = rh.Key_Abs_Loan
       AND lhk.[Type] = 1
    INNER JOIN [Ehtirom].[dbo].[LOAN] AS l
        ON l.App = lhk.[App]
       AND l.UAN IN ('01', '02', '03', '04')
    INNER JOIN [Ehtirom].[dbo].[CUSTOMER] AS c
        ON c.Id = l.Customer_ID
    WHERE rh.Kib_Id = '06'
      AND rh.[Status] NOT IN ('04', '09')
      AND DATEDIFF(SECOND, rh.[Next_Access_Date], GETDATE()) >= 0
      AND NOT (
            rh.[Status] IN ('02', '03')
            AND NULLIF(LTRIM(RTRIM(rh.Token)), '') IS NULL
      );
END;
GO
