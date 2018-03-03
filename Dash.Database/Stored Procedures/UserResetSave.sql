
CREATE PROCEDURE UserResetSave
	@Id INT,
	@ResetHash NVARCHAR(500) = NULL,
	@DateReset DATETIMEOFFSET = NULL,
	@RequestUserId INT = NULL
 AS
	SET NOCOUNT ON

	BEGIN TRY
		UPDATE [User] SET ResetHash = @ResetHash, DateReset = @DateReset, UserUpdated = @RequestUserId, DateUpdated = SYSDATETIMEOFFSET() WHERE Id = @Id
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
