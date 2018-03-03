CREATE PROCEDURE DatasetColumnDelete
	@Id INT,
	@RequestUserId INT = NULL
AS
	SET NOCOUNT ON
	SET XACT_ABORT ON

	BEGIN TRY
		BEGIN TRAN
			DELETE FROM ReportColumn WHERE ColumnId = @Id
			DELETE FROM ReportFilter WHERE ColumnId = @Id
			-- update the xaxis and yaxis columns if needed
			UPDATE Report SET XAxisColumnId = NULL, UserUpdated = @RequestUserId, DateUpdated = SYSDATETIMEOFFSET() WHERE XAxisColumnId = @Id
			UPDATE Report SET YAxisColumnId = NULL, UserUpdated = @RequestUserId, DateUpdated = SYSDATETIMEOFFSET() WHERE YAxisColumnId = @Id
			UPDATE ChartRange SET XAxisColumnId = NULL, UserUpdated = @RequestUserId, DateUpdated = SYSDATETIMEOFFSET() WHERE XAxisColumnId = @Id
			UPDATE ChartRange SET YAxisColumnId = NULL, UserUpdated = @RequestUserId, DateUpdated = SYSDATETIMEOFFSET() WHERE YAxisColumnId = @Id
			-- delete the column
			DELETE FROM DatasetColumn WHERE Id = @Id
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