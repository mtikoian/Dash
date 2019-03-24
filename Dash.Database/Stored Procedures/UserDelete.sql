CREATE PROCEDURE UserDelete
    @Id INT,
    @RequestUserId INT = NULL
AS
    SET NOCOUNT ON

    UPDATE [User] SET [Status] = 0, UserUpdated = @RequestUserId, DateUpdated = SYSDATETIMEOFFSET() WHERE Id = @Id
    RETURN 0
