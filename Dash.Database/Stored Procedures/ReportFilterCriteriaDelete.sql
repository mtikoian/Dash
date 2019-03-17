CREATE PROCEDURE ReportFilterCriteriaDelete
    @Id INT,
    @RequestUserId INT = NULL
AS
    SET NOCOUNT ON

    BEGIN TRY
        BEGIN TRAN
            UPDATE r SET DateUpdated = SYSDATETIMEOFFSET(), UserUpdated = @RequestUserId
                FROM Report r
                INNER JOIN ReportFilter rf ON rf.ReportId = r.Id
                INNER JOIN ReportFilterCriteria rfc ON rfc.ReportFilterId = rf.Id
                WHERE rfc.Id = @Id
            DELETE FROM ReportFilterCriteria WHERE Id = @Id
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
