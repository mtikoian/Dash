CREATE PROCEDURE UserLoginAttemptsSave
    @Id INT,
    @LoginAttempts INT,
    @DateUnlocks DATETIMEOFFSET,
    @RequestUserId INT = NULL
 AS
    SET NOCOUNT ON

    UPDATE [User] SET LoginAttempts = @LoginAttempts, DateUnlocks = @DateUnlocks,
        UserUpdated = COALESCE(@RequestUserId, @Id, UserUpdated), DateUpdated = SYSDATETIMEOFFSET()
    WHERE Id = @Id
