CREATE PROCEDURE UserLoginAttemptsSave
    @Id INT,
    @LoginAttempts INT,
    @DateUnlocks DATETIMEOFFSET,
    @SessionId VARCHAR(50) = NULL,
    @RequestUserId INT = NULL
 AS
    SET NOCOUNT ON

    UPDATE [User] SET LoginAttempts = @LoginAttempts, DateUnlocks = @DateUnlocks, SessionId = ISNULL(@SessionId, SessionId),
        UserUpdated = COALESCE(@RequestUserId, @Id, UserUpdated), DateUpdated = SYSDATETIMEOFFSET()
    WHERE Id = @Id
