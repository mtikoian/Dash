CREATE PROCEDURE UserGet
	@Id INT = NULL,
	@UID NVARCHAR(250) = NULL,
	@Email NVARCHAR(100) = NULL,
	@IsActive BIT = NULL
AS
	SET NOCOUNT ON
	
	DECLARE @Ids TABLE (Id INT NOT NULL PRIMARY KEY)
	IF (@Id IS NOT NULL)
		INSERT INTO @Ids SELECT @Id
	ELSE IF (@UID IS NOT NULL)
		INSERT INTO @Ids SELECT Id FROM [User] WHERE [UID] = @UID
	ELSE IF (@Email IS NOT NULL)
		INSERT INTO @Ids SELECT Id FROM [User] WHERE Email = @Email
	ELSE
		INSERT INTO @Ids SELECT ID FROM [User]

	SELECT u.Id, [UID], FirstName, LastName, LanguageId, IsActive, Email, [Password], Salt, ResetHash, DateReset, l.LanguageCode 
	FROM @Ids i
	INNER JOIN [User] u ON u.Id = i.Id
	LEFT JOIN [Language] l ON l.Id = u.LanguageId
	WHERE ISNULL(@IsActive, IsActive) = IsActive AND IsDeleted = 0
	ORDER BY [UID]