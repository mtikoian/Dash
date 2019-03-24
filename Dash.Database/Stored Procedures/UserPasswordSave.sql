CREATE PROCEDURE UserPasswordSave
    @Id INT,
    @Password NVARCHAR(100),
    @Salt NVARCHAR(100),
    @RequestUserId INT = NULL
 AS
    SET NOCOUNT ON

    UPDATE [User] SET [Password] = @Password, Salt = @Salt,
        UserUpdated = ISNULL(@RequestUserId, UserUpdated), DateUpdated = SYSDATETIMEOFFSET()
    WHERE Id = @Id
