CREATE PROCEDURE DatasetDelete
    @Id INT,
    @RequestUserId INT = NULL
AS
    SET NOCOUNT ON
    SET XACT_ABORT ON

    BEGIN TRY
        BEGIN TRAN
            /* delete any reports pointing at this dataset */
            DECLARE @DeleteId INT
            DECLARE db_cursor CURSOR FAST_FORWARD FOR
            SELECT Id FROM Report WHERE DatasetId = @Id
            OPEN db_cursor
            FETCH NEXT FROM db_cursor INTO @DeleteId
            WHILE @@FETCH_STATUS = 0
            BEGIN
                EXEC ReportDelete @DeleteId, @RequestUserId
                FETCH NEXT FROM db_cursor INTO @DeleteId
            END
            CLOSE db_cursor
            DEALLOCATE db_cursor

            DELETE FROM DatasetColumn WHERE DatasetId = @Id
            DELETE FROM DatasetJoin WHERE DatasetId = @Id
            DELETE FROM DatasetRole WHERE DatasetId = @Id
            DELETE FROM Dataset WHERE Id = @Id
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
