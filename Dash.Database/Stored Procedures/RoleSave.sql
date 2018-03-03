CREATE PROCEDURE RoleSave
	@Id INT OUTPUT,
	@Name NVARCHAR(100),
	@RequestUserId INT = NULL
AS
	SET NOCOUNT ON

	BEGIN TRY
		IF ISNULL(@Id, 0) = 0
			BEGIN
				INSERT INTO Role (Name, UserCreated) VALUES (@Name, @RequestUserId)
				SET @Id = SCOPE_IDENTITY()
			END
		ELSE
			BEGIN
				UPDATE Role SET Name = @Name, UserUpdated = @RequestUserId, DateUpdated = SYSDATETIMEOFFSET() WHERE Id = @Id
			END
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