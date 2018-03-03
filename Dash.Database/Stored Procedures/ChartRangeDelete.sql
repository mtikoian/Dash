CREATE PROCEDURE ChartRangeDelete
	@Id INT,
	@RequestUserId INT = NULL
AS
	SET NOCOUNT ON

	BEGIN TRY
		BEGIN TRAN
			UPDATE c SET DateUpdated = SYSDATETIMEOFFSET(), UserUpdated = @RequestUserId
			FROM Chart c
			INNER JOIN ChartRange cd ON cd.ChartId = c.Id
			WHERE cd.Id = @Id

			DELETE FROM ChartRange WHERE Id = @Id
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