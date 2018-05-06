CREATE PROCEDURE UserMembershipGet
	@UserName NVARCHAR(100) = NULL,
    @Email NVARCHAR(100) = NULL
 AS
	SET NOCOUNT ON

	SELECT Id, UserName, FirstName, LastName, LanguageId, [Password], Salt, IsActive, LoginAttempts, DateUnlocks, AllowSingleFactor, Email, DateReset, ResetHash
    FROM [User] 
    WHERE ((@UserName IS NOT NULL AND UserName = @UserName ) OR (@Email IS NOT NULL AND Email = @Email ))
        AND [Status] = 1 AND IsDeleted = 0
