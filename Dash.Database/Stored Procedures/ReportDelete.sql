CREATE PROCEDURE ReportDelete
	@Id INT,
	@RequestUserId INT = NULL
AS
	SET NOCOUNT ON
	SET XACT_ABORT ON

	BEGIN TRY
		BEGIN TRAN
			DELETE FROM ReportFilter WHERE ReportId = @Id
			DELETE FROM ReportColumn WHERE ReportId = @Id
			DELETE FROM ReportShare WHERE ReportId = @Id
			DELETE FROM ChartRange WHERE ReportId = @Id
			DELETE FROM Widget WHERE ReportId = @Id
			DELETE FROM Report WHERE Id = @Id
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
