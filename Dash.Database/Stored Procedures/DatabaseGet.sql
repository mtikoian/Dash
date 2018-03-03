CREATE PROCEDURE DatabaseGet
	@Id INT = NULL,
	@Name NVARCHAR(100) = NULL
AS
	SET NOCOUNT ON
	
	DECLARE @Ids TABLE (Id INT NOT NULL PRIMARY KEY)
	IF (@Id IS NOT NULL)
		INSERT INTO @Ids SELECT @Id
	ELSE IF (@Name IS NOT NULL)
		INSERT INTO @Ids SELECT Id FROM [Database] WHERE Name = @Name
	ELSE
		INSERT INTO @Ids SELECT Id FROM [Database]

	SELECT d.Id, Name, Host, [User], [Password], DatabaseName, Port, AllowPaging, ConnectionString, TypeId
	FROM @Ids i
	INNER JOIN [Database] d ON d.Id = i.Id
	ORDER BY Name