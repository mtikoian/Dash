CREATE PROCEDURE ReportColumnDelete
    @Id INT,
    @RequestUserId INT = NULL
AS
    SET NOCOUNT ON

    BEGIN TRY
        BEGIN TRAN
            UPDATE r SET DateUpdated = SYSDATETIMEOFFSET(), UserUpdated = @RequestUserId
            FROM Report r
            INNER JOIN ReportColumn rc ON rc.ReportId = r.Id
            WHERE rc.Id = @Id

            DELETE FROM ReportColumn WHERE Id = @Id
        COMMIT
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