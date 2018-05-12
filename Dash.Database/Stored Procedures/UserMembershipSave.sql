CREATE PROCEDURE UserMembershipSave
	@Id INT OUTPUT,
	@UserName NVARCHAR(100),
	@Email NVARCHAR(100),
	@Password NVARCHAR(500),
	@Salt NVARCHAR(500),
	@RequestUserId INT = NULL
 AS
	SET NOCOUNT ON

	SELECT @Id = Id FROM [User] WHERE UserName = @UserName AND [Status] = 1

	IF ISNULL(@Id, 0) = 0
		BEGIN
			INSERT INTO [User] (UserName, Email, [Password], Salt, UserCreated) VALUES (@UserName, @Email, @Password, @Salt, @RequestUserId)
			SET @Id = SCOPE_IDENTITY()
		END
	ELSE
		BEGIN
			UPDATE [User] SET UserName = @UserName, Email = @Email, [Password] = @Password, Salt = @Salt, UserUpdated = @RequestUserId, DateUpdated = SYSDATETIMEOFFSET() WHERE Id = @Id
		END
	RETURN 0
