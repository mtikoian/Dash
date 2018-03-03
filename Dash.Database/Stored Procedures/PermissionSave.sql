CREATE PROCEDURE PermissionSave
	@Id INT OUTPUT,
	@ControllerName NVARCHAR(100),
	@ActionName NVARCHAR(100),
	@RequestUserId INT = NULL
AS
	SET NOCOUNT ON

	BEGIN TRY
		IF ISNULL(@Id, 0) = 0
			BEGIN
				INSERT INTO Permission (ControllerName, ActionName, UserCreated) VALUES (@ControllerName, @ActionName, @RequestUserId)
				SET @Id = SCOPE_IDENTITY()
			END
		ELSE
			BEGIN
				UPDATE Permission SET ControllerName = @ControllerName, ActionName = @ActionName, 
					UserUpdated = @RequestUserId, DateUpdated = SYSDATETIMEOFFSET() 
				WHERE Id = @Id
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