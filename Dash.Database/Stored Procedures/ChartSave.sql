CREATE PROCEDURE ChartSave
    @Id INT OUTPUT,
    @Name NVARCHAR(100),
    @ChartTypeId INT = NULL,
    @RequestUserId INT = NULL
AS
    SET NOCOUNT ON

    BEGIN TRY
        IF ISNULL(@Id, 0) = 0
            BEGIN
                INSERT INTO Chart ([Name], ChartTypeId, UserCreated)
                    VALUES (@Name, (CASE @ChartTypeId WHEN 0 THEN NULL ELSE @ChartTypeId END), @RequestUserId)
                SET @Id = SCOPE_IDENTITY()
            END
        ELSE
            BEGIN
                UPDATE Chart SET [Name] = @Name, ChartTypeId = (CASE @ChartTypeId WHEN 0 THEN NULL ELSE @ChartTypeId END),
                    UserUpdated = @RequestUserId, DateUpdated = SYSDATETIMEOFFSET()
                WHERE Id = @Id
            END
        RETURN 0
    END TRY
    BEGIN CATCH
        IF XACT_STATE() = -1 ROLLBACK
        DECLARE @ErrorMessage NVARCHAR(4000), @ErrorSeverity INT, @ErrorState INT
        SET @ErrorMessage = CAST (ERROR_NUMBER() AS VARCHAR) + ': ' + ERROR_MESSAGE()
        SET @ErrorSeverity = ERROR_SEVERITY()
        SET @ErrorState = ERROR_STATE()
        RAISERROR (@ErrorMessage, @ErrorSeverity, @ErrorState)
    END CATCH
