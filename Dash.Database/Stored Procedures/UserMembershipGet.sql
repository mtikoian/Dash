CREATE PROCEDURE UserMembershipGet
    @UserName NVARCHAR(100) = NULL,
    @Email NVARCHAR(100) = NULL
 AS
    SET NOCOUNT ON

    SELECT u.Id, u.UserName, u.FirstName, u.LastName, u.LanguageId, u.[Password], u.Salt, u.LoginAttempts, u.SessionId, u.DateUnlocks, u.AllowSingleFactor, u.Email, u.DateReset, u.ResetHash, u.DateLoginWindow, u.LoginHash, l.LanguageCode
    FROM [User] u
    INNER JOIN [Language] l ON l.Id = u.LanguageId
    WHERE ((@UserName IS NOT NULL AND u.UserName = @UserName ) OR (@Email IS NOT NULL AND u.Email = @Email ))
        AND u.[Status] = 1
