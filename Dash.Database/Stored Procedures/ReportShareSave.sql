CREATE PROCEDURE ReportShareSave
	@Id INT OUTPUT,
	@ReportId INT,
	@UserId INT,
	@RoleId INT,
	@RequestUserId INT = NULL
AS
	SET NOCOUNT ON

	BEGIN TRY
		BEGIN TRAN
			UPDATE Report Set DateUpdated = SYSDATETIMEOFFSET(), UserUpdated = @RequestUserId WHERE Id = @ReportId

			IF ISNULL(@Id, 0) = 0
				BEGIN
					INSERT INTO ReportShare (ReportId, UserId, RoleId, UserCreated) VALUES (@ReportId, @UserId, @RoleId, @RequestUserId)
					SET @Id = SCOPE_IDENTITY()
				END
			ELSE
				BEGIN
					UPDATE ReportShare SET ReportId = @ReportId, UserId = @UserId, RoleId = @RoleId, UserUpdated = @RequestUserId, DateUpdated = SYSDATETIMEOFFSET() WHERE Id = @Id
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