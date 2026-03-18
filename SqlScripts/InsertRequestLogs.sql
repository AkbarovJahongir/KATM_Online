CREATE OR ALTER PROCEDURE [dbo].[InsertRequestLogs]
    @RequestURL NVARCHAR(256),
    @RequestBody NVARCHAR(MAX),
    @HttpMethod VARCHAR(10),
    @ResponseCode VARCHAR(3),
    @ResponseBody NVARCHAR(MAX),
    @DateRequest DATETIME,
    @DateResponse DATETIME,
    @ResultCode VARCHAR(2) = NULL OUTPUT,
    @ResultMessage NVARCHAR(200) = NULL OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        INSERT INTO [dbo].[RequestLogs]
        (
            [RequestUrl],
            [RequestBody],
            [HttpMethod],
            [ResponseCode],
            [ResponseBody],
            [DateRequest],
            [DateResponse]
        )
        VALUES
        (
            @RequestURL,
            @RequestBody,
            @HttpMethod,
            @ResponseCode,
            @ResponseBody,
            @DateRequest,
            @DateResponse
        );

        SET @ResultCode = '0';
        SET @ResultMessage = N'OK';
    END TRY
    BEGIN CATCH
        SET @ResultCode = '1';
        SET @ResultMessage = ERROR_MESSAGE();
    END CATCH
END;
GO
