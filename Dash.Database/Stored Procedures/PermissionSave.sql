CREATE PROCEDURE PermissionSave
	@Id INT OUTPUT,
	@ControllerName NVARCHAR(100),
	@ActionName NVARCHAR(100),
	@RequestUserId INT = NULL
AS
	SET NOCOUNT ON

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
