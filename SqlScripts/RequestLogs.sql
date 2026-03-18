CREATE TABLE [dbo].[RequestLogs] (
    [Key] INT IDENTITY(1,1) NOT NULL,
    [RequestUrl] NVARCHAR(256) NOT NULL,
    [RequestBody] NVARCHAR(MAX) NULL,
    [HttpMethod] VARCHAR(10) NOT NULL,
    [ResponseCode] VARCHAR(3) NULL,
    [ResponseBody] NVARCHAR(MAX) NOT NULL,
    [DateIn] DATETIME NULL CONSTRAINT [DF_RequestLogs_DateIn] DEFAULT (GETDATE()),
    [DateRequest] DATETIME NOT NULL,
    [DateResponse] DATETIME NOT NULL,
    CONSTRAINT [PK_RequestLogs_Key] PRIMARY KEY CLUSTERED ([Key] ASC)
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY];
GO
