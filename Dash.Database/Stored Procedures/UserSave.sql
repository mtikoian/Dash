CREATE PROCEDURE UserSave
    @Id INT OUTPUT,
    @UserName NVARCHAR(100),
    @FirstName NVARCHAR(100),
    @LastName NVARCHAR(100),
    @LanguageId INT,
    @Email NVARCHAR(100),
    @AllowSingleFactor BIT,
    @RequestUserId INT = NULL
AS
    SET NOCOUNT ON

    IF ISNULL(@Id, 0) = 0
        BEGIN
            INSERT INTO [User] (UserName, FirstName, LastName, [LanguageId], Email, AllowSingleFactor, UserCreated)
                VALUES (@UserName, @FirstName, @LastName, @LanguageId, @Email, @AllowSingleFactor, @RequestUserId)
            SET @Id = SCOPE_IDENTITY()
        END
    ELSE
        BEGIN
            UPDATE [User] SET UserName = @UserName, FirstName = @FirstName, LastName = @LastName, [LanguageId] = @LanguageId,
                Email = @Email, AllowSingleFactor = @AllowSingleFactor, UserUpdated = @RequestUserId, DateUpdated = SYSDATETIMEOFFSET() WHERE Id = @Id
        END
    RETURN 0
