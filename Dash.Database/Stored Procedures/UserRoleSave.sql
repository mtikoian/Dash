CREATE PROCEDURE UserRoleSave
	@Id INT OUTPUT,
	@UserId INT,
	@RoleId INT,
	@RequestUserId INT = NULL
AS
	SET NOCOUNT ON

	BEGIN TRY
		IF ISNULL(@Id, 0) = 0
			BEGIN
				INSERT INTO UserRole (UserId, RoleId, UserCreated) VALUES (@UserId, @RoleId, @RequestUserId)
				SET @Id = SCOPE_IDENTITY()
			END
		ELSE
			BEGIN
				UPDATE UserRole SET UserId = @UserId, RoleId = @RoleId, UserUpdated = @RequestUserId, DateUpdated = SYSDATETIMEOFFSET() WHERE Id = @Id
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