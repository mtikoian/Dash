CREATE PROCEDURE ReportColumnSave
    @Id INT OUTPUT,
    @ReportId INT,
    @ColumnId INT,
    @DisplayOrder INT = NULL,
    @Width DECIMAL(18,14) = NULL,
    @SortOrder INT = NULL,
    @SortDirection NVARCHAR(10) = NULL,
    @RequestUserId INT = NULL
 AS
    SET NOCOUNT ON

    BEGIN TRY
        BEGIN TRAN
            UPDATE Report SET DateUpdated = SYSDATETIMEOFFSET(), UserUpdated = @RequestUserId WHERE Id = @ReportId

            IF ISNULL(@Id, 0) = 0
                BEGIN
                    INSERT INTO ReportColumn (ReportId, ColumnId, DisplayOrder, Width, SortOrder, SortDirection, UserCreated)
                        VALUES (@ReportId, @ColumnId, @DisplayOrder, @Width, @SortOrder, @SortDirection, @RequestUserId)
                    SET @Id = SCOPE_IDENTITY()
                END
            ELSE
                BEGIN
                    UPDATE ReportColumn SET    ReportId = @ReportId, ColumnId = @ColumnId, DisplayOrder = @DisplayOrder, SortOrder = @SortOrder,
                        SortDirection = @SortDirection, Width = @Width, UserUpdated = @RequestUserId, DateUpdated = SYSDATETIMEOFFSET()
                    WHERE Id = @Id
                END
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
