CREATE TABLE [dbo].[Ci017RequestLog] (
    Id            INT IDENTITY PRIMARY KEY,
    LoanKey       INT NOT NULL,
    ClaimId       NVARCHAR(100) NULL,
    RequestType   VARCHAR(20) NOT NULL,   -- 'Report' | 'StatusRequest'
    AttemptNumber INT NOT NULL,
    RequestBody   NVARCHAR(MAX) NULL,
    ResponseBody  NVARCHAR(MAX) NULL,
    DateRequest   DATETIME2 NOT NULL,
    DateResponse  DATETIME2 NULL,
    CreatedAt     DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
GO

CREATE INDEX IX_Ci017RequestLog_LoanKey ON [dbo].[Ci017RequestLog] (LoanKey);
GO
