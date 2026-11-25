USE [CIB];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

ALTER PROCEDURE [dbo].[Loan_History_KB_KatmHelper_Xml]
    @KeyLoanHistory NVARCHAR(20),
    @Text VARCHAR(MAX),
    @TypeOperation VARCHAR(50),
    @RequestSubtype VARCHAR(30) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF ISNULL(@KeyLoanHistory, '') = ''
       OR ISNULL(@Text, '') = ''
       OR ISNULL(@TypeOperation, '') = ''
        RETURN;

    DECLARE @ParentComment NVARCHAR(500) = LEFT(
        CASE WHEN @RequestSubtype IS NULL
             THEN @Text
             ELSE CONCAT(N'Отчёт CI-017 ', @RequestSubtype, N': ', @Text)
        END, 500);
    DECLARE @SuccessComment NVARCHAR(500) = LEFT(
        CASE WHEN @RequestSubtype IS NULL
             THEN N'Отчёт CI-017 успешно получен.'
             ELSE CONCAT(N'Отчёт CI-017 ', @RequestSubtype, N' успешно получен.')
        END, 500);

    BEGIN TRANSACTION;
    BEGIN TRY
        IF LOWER(@TypeOperation) = LOWER('IndividualRequestSuccessful')
        BEGIN
            UPDATE [dbo].[Request_History_Xml]
            SET [Status] = '02', KATMSIR = @Text
            WHERE [Key_Abs_Loan] = @KeyLoanHistory AND [Status] = '00';
        END
        ELSE IF LOWER(@TypeOperation) = LOWER('Token')
        BEGIN
            UPDATE [dbo].[Request_History_Xml]
            SET [Token] = @Text, [Status] = '03', [Send_Date] = GETDATE()
            WHERE [Key_Abs_Loan] = @KeyLoanHistory
              AND (@RequestSubtype IS NULL OR [Request_Subtype] = @RequestSubtype);

            IF @@ROWCOUNT < 1
            BEGIN
                ROLLBACK TRANSACTION;
                RETURN;
            END;

            UPDATE [Imkon_Finance].[dbo].[Loan_History_KB]
            SET [Comment] = CASE WHEN @RequestSubtype IS NULL
                THEN N'CI-017: получен токен, ожидается готовность отчёта.'
                ELSE CONCAT(N'Отчёт CI-017 ', @RequestSubtype, N': получен токен, ожидается готовность отчёта.')
            END
            WHERE [key] = @KeyLoanHistory;
        END
        ELSE IF LOWER(@TypeOperation) = LOWER('Base64')
        BEGIN
            UPDATE [dbo].[Request_History_Xml]
            SET [ResponseBase64] = @Text, [Status] = '04', [Get_Date] = GETDATE()
            WHERE [Key_Abs_Loan] = @KeyLoanHistory
              AND (@RequestSubtype IS NULL OR [Request_Subtype] = @RequestSubtype);

            IF @@ROWCOUNT < 1
            BEGIN
                ROLLBACK TRANSACTION;
                RETURN;
            END;

            -- Сразу отмечаем успешный конкретный отчёт, не дожидаясь остальных типов.
            UPDATE [Imkon_Finance].[dbo].[Loan_History_KB]
            SET [Comment] = @SuccessComment
            WHERE [key] = @KeyLoanHistory;

            IF NOT EXISTS (
                SELECT 1 FROM [dbo].[Request_History_Xml]
                WHERE [Key_Abs_Loan] = @KeyLoanHistory AND [Status] NOT IN ('04', '09')
            )
            BEGIN
                UPDATE [Imkon_Finance].[dbo].[Loan_History_KB]
                SET [Status] = CASE WHEN EXISTS (
                    SELECT 1 FROM [dbo].[Request_History_Xml]
                    WHERE [Key_Abs_Loan] = @KeyLoanHistory AND [Status] = '09'
                ) THEN '09' ELSE '03' END,
                    [Get_Date] = GETDATE()
                WHERE [key] = @KeyLoanHistory;
            END;
        END
        ELSE IF LOWER(@TypeOperation) = LOWER('Error')
        BEGIN
            UPDATE [dbo].[Request_History_Xml]
            SET [Status] = '09', [Get_Date] = GETDATE(), [Comment] = @Text
            WHERE [Key_Abs_Loan] = @KeyLoanHistory
              AND (@RequestSubtype IS NULL OR [Request_Subtype] = @RequestSubtype);

            IF NOT EXISTS (
                SELECT 1 FROM [dbo].[Request_History_Xml]
                WHERE [Key_Abs_Loan] = @KeyLoanHistory AND [Status] NOT IN ('04', '09')
            )
            BEGIN
                UPDATE [Imkon_Finance].[dbo].[Loan_History_KB]
                SET [Status] = CASE WHEN EXISTS (
                    SELECT 1 FROM [dbo].[Request_History_Xml]
                    WHERE [Key_Abs_Loan] = @KeyLoanHistory AND [Status] = '09'
                ) THEN '09' ELSE '03' END,
                    [Get_Date] = GETDATE(), [Comment] = @ParentComment
                WHERE [key] = @KeyLoanHistory;
            END;

            UPDATE [Imkon_Finance].[dbo].[Loan_History_KB]
            SET [Comment] = @ParentComment
            WHERE [key] = @KeyLoanHistory;
        END
        ELSE IF LOWER(@TypeOperation) = LOWER('Comment')
        BEGIN
            UPDATE [Imkon_Finance].[dbo].[Loan_History_KB]
            SET [Comment] = @ParentComment
            WHERE [key] = @KeyLoanHistory;
        END
        ELSE IF LOWER(@TypeOperation) = LOWER('AddNextAccess')
        BEGIN
            UPDATE [dbo].[Request_History_Xml]
            SET [Next_Access_Date] = DATEADD(MINUTE, CONVERT(INT, @Text), GETDATE()),
                [QuantitySelected] = ISNULL([QuantitySelected], 0) + 1
            WHERE [Key_Abs_Loan] = @KeyLoanHistory
              AND (@RequestSubtype IS NULL OR [Request_Subtype] = @RequestSubtype);

            UPDATE [Imkon_Finance].[dbo].[Loan_History_KB]
            SET [Comment] = CASE WHEN @RequestSubtype IS NULL
                THEN CONCAT(N'CI-017: повторная проверка через ', @Text, N' мин.')
                ELSE CONCAT(N'Отчёт CI-017 ', @RequestSubtype, N': повторная проверка через ', @Text, N' мин.')
            END
            WHERE [key] = @KeyLoanHistory;
        END;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
