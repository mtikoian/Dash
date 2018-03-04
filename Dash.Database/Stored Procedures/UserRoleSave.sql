CREATE PROCEDURE UserRoleSave
	@Id INT OUTPUT,
	@UserId INT,
	@RoleId INT,
	@RequestUserId INT = NULL
AS
	SET NOCOUNT ON

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
