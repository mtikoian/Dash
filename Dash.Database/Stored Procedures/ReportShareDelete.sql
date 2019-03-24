CREATE PROCEDURE ReportShareDelete
    @Id INT,
    @RequestUserId INT = NULL
AS
    SET NOCOUNT ON

    BEGIN TRY
        BEGIN TRAN
            UPDATE r SET DateUpdated = SYSDATETIMEOFFSET(), UserUpdated = @RequestUserId
            FROM Report r
            INNER JOIN ReportShare rs ON rs.ReportId = r.Id
            WHERE rs.Id = @Id

            DELETE FROM ReportShare WHERE Id = @Id
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