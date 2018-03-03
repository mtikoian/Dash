CREATE PROCEDURE DatabaseDelete
	@Id INT,
	@RequestUserId INT = NULL
AS
	SET NOCOUNT ON
	SET XACT_ABORT ON

	BEGIN TRY
		BEGIN TRAN
			UPDATE [Dataset] SET DatabaseId = NULL, UserUpdated = @RequestUserId, DateUpdated = SYSDATETIMEOFFSET() WHERE DatabaseId = @Id
			DELETE FROM [Database] WHERE Id = @Id
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