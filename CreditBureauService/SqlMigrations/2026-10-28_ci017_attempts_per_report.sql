/* Apply in the CIB database used by the running service. */
USE [CIB];
GO

IF OBJECT_ID(N'dbo.Ci017AttemptByReport', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Ci017AttemptByReport
    (
        LoanKey INT NOT NULL,
        RequestSubtype VARCHAR(30) NOT NULL,
        AttemptCount INT NOT NULL CONSTRAINT DF_Ci017AttemptByReport_AttemptCount DEFAULT (0),
        LastStatus NVARCHAR(10) NULL,
        LastAttemptAt DATETIME2 NULL,
        CONSTRAINT PK_Ci017AttemptByReport PRIMARY KEY (LoanKey, RequestSubtype)
    );
END;
GO
