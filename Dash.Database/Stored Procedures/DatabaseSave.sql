CREATE PROCEDURE DatabaseSave
	@Id INT OUTPUT,
	@Name NVARCHAR(100),
	@DatabaseName NVARCHAR(100),
	@Host NVARCHAR(100) = NULL,
	@TypeId TINYINT = NULL,
	@Port NVARCHAR(50) = NULL,
	@User NVARCHAR(100) = NULL,
	@Password NVARCHAR(500) = NULL,
	@AllowPaging BIT = 1,
	@ConnectionString NVARCHAR(500) = NULL,
	@RequestUserId INT = NULL
AS
	SET NOCOUNT ON

	IF ISNULL (@Id, 0) = 0
		BEGIN
			INSERT INTO [Database] (Name, Host, TypeId, Port, [User], [Password], DatabaseName, AllowPaging, ConnectionString, UserCreated)
				VALUES (@Name, @Host, @TypeId, @Port, @User, @Password, @DatabaseName, @AllowPaging, @ConnectionString, @RequestUserId) 
			SET @Id = SCOPE_IDENTITY()
		END
	ELSE
		BEGIN
			UPDATE [Database] SET Name = @Name, Host = @Host, TypeId = @TypeId, Port = @Port, [User] = @User, [Password] = @Password, DatabaseName = @DatabaseName, 
				AllowPaging = @AllowPaging, ConnectionString = @ConnectionString, 
				UserUpdated = @RequestUserId, DateUpdated = SYSDATETIMEOFFSET()
			WHERE Id = @Id
		END
	RETURN 0