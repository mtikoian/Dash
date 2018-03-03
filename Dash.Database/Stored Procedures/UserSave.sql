CREATE PROCEDURE UserSave
	@Id INT OUTPUT,
	@UID NVARCHAR(100),
	@FirstName NVARCHAR(100),
	@LastName NVARCHAR(100),
	@LanguageId INT,
	@IsActive BIT,
	@Email NVARCHAR(100),
	@RequestUserId INT = NULL
AS
	SET NOCOUNT ON

	IF ISNULL(@Id, 0) = 0
		BEGIN
			INSERT INTO [User] ([UID], FirstName, LastName, [LanguageId], IsActive, Email, UserCreated)
				VALUES (@UID, @FirstName, @LastName, @LanguageId, @IsActive, @Email, @RequestUserId)
			SET @Id = SCOPE_IDENTITY()
		END
	ELSE
		BEGIN
			UPDATE [User] SET [UID] = @UID, FirstName = @FirstName, LastName = @LastName, [LanguageId] = @LanguageId, IsActive = @IsActive, 
				Email = @Email, UserUpdated = @RequestUserId, DateUpdated = SYSDATETIMEOFFSET() WHERE Id = @Id
		END
	RETURN 0