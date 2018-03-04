CREATE PROCEDURE RoleSave
	@Id INT OUTPUT,
	@Name NVARCHAR(100),
	@RequestUserId INT = NULL
AS
	SET NOCOUNT ON

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
