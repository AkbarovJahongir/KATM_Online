CREATE OR ALTER PROCEDURE [dbo].[Katm_Methods_Request_UpsertCiStatus]
    @loanKey INT,
    @ciCode INT,
    @ciStatus TINYINT,
    @message NVARCHAR(500) = NULL,
    @token VARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @ciCode NOT IN (1, 2, 3, 4, 5, 11, 12, 13, 14, 15, 16, 17, 18, 20, 21, 22, 23)
    BEGIN
        THROW 50001, 'Unsupported ciCode for Katm_Methods_Request update.', 1;
    END;

    IF @ciStatus NOT IN (0, 1, 2)
    BEGIN
        THROW 50002, 'Unsupported ciStatus. Allowed values: 0, 1, 2.', 1;
    END;

    IF NOT EXISTS (SELECT 1 FROM [dbo].[Katm_Methods_Request] WHERE [loanKey] = @loanKey)
    BEGIN
        INSERT INTO [dbo].[Katm_Methods_Request] ([loanKey])
        VALUES (@loanKey);
    END;

    UPDATE [dbo].[Katm_Methods_Request]
    SET [updatedAt] = GETDATE(),
        [ci001] = CASE WHEN @ciCode = 1 THEN @ciStatus ELSE [ci001] END,
        [ci001Message] = CASE WHEN @ciCode = 1 THEN @message ELSE [ci001Message] END,
        [ci002] = CASE WHEN @ciCode = 2 THEN @ciStatus ELSE [ci002] END,
        [ci002Message] = CASE WHEN @ciCode = 2 THEN @message ELSE [ci002Message] END,
        [ci003] = CASE WHEN @ciCode = 3 THEN @ciStatus ELSE [ci003] END,
        [ci003Message] = CASE WHEN @ciCode = 3 THEN @message ELSE [ci003Message] END,
        [ci004] = CASE WHEN @ciCode = 4 THEN @ciStatus ELSE [ci004] END,
        [ci004Message] = CASE WHEN @ciCode = 4 THEN @message ELSE [ci004Message] END,
        [ci005] = CASE WHEN @ciCode = 5 THEN @ciStatus ELSE [ci005] END,
        [ci005Message] = CASE WHEN @ciCode = 5 THEN @message ELSE [ci005Message] END,
        [ci011] = CASE WHEN @ciCode = 11 THEN @ciStatus ELSE [ci011] END,
        [ci011Message] = CASE WHEN @ciCode = 11 THEN @message ELSE [ci011Message] END,
        [ci012] = CASE WHEN @ciCode = 12 THEN @ciStatus ELSE [ci012] END,
        [ci012Message] = CASE WHEN @ciCode = 12 THEN @message ELSE [ci012Message] END,
        [ci013] = CASE WHEN @ciCode = 13 THEN @ciStatus ELSE [ci013] END,
        [ci013Message] = CASE WHEN @ciCode = 13 THEN @message ELSE [ci013Message] END,
        [ci014] = CASE WHEN @ciCode = 14 THEN @ciStatus ELSE [ci014] END,
        [ci014Message] = CASE WHEN @ciCode = 14 THEN @message ELSE [ci014Message] END,
        [ci015] = CASE WHEN @ciCode = 15 THEN @ciStatus ELSE [ci015] END,
        [ci015Message] = CASE WHEN @ciCode = 15 THEN @message ELSE [ci015Message] END,
        [ci016] = CASE WHEN @ciCode = 16 THEN @ciStatus ELSE [ci016] END,
        [ci016Message] = CASE WHEN @ciCode = 16 THEN @message ELSE [ci016Message] END,
        [ci017] = CASE WHEN @ciCode = 17 THEN @ciStatus ELSE [ci017] END,
        [ci017Message] = CASE WHEN @ciCode = 17 THEN @message ELSE [ci017Message] END,
        [ci017Token] = CASE WHEN @ciCode = 17 THEN @token ELSE [ci017Token] END,
        [ci018] = CASE WHEN @ciCode = 18 THEN @ciStatus ELSE [ci018] END,
        [ci018Message] = CASE WHEN @ciCode = 18 THEN @message ELSE [ci018Message] END,
        [ci020] = CASE WHEN @ciCode = 20 THEN @ciStatus ELSE [ci020] END,
        [ci020Message] = CASE WHEN @ciCode = 20 THEN @message ELSE [ci020Message] END,
        [ci021] = CASE WHEN @ciCode = 21 THEN @ciStatus ELSE [ci021] END,
        [ci021Message] = CASE WHEN @ciCode = 21 THEN @message ELSE [ci021Message] END,
        [ci022] = CASE WHEN @ciCode = 22 THEN @ciStatus ELSE [ci022] END,
        [ci022Message] = CASE WHEN @ciCode = 22 THEN @message ELSE [ci022Message] END,
        [ci023] = CASE WHEN @ciCode = 23 THEN @ciStatus ELSE [ci023] END,
        [ci023Message] = CASE WHEN @ciCode = 23 THEN @message ELSE [ci023Message] END
    WHERE [loanKey] = @loanKey;
END;
GO
