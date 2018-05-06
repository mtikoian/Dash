CREATE PROCEDURE UserRoleGet
    @Id INT = NULL,
    @UserName NVARCHAR(100) = NULL,
    @RoleId INT = NULL,
    @UserId INT = NULL
AS
    SET NOCOUNT ON

    DECLARE @Ids TABLE (Id INT NOT NULL PRIMARY KEY)
    IF (@Id IS NOT NULL)
        INSERT INTO @Ids SELECT @Id
    ELSE IF (@UserName IS NOT NULL)
        INSERT INTO @Ids SELECT DISTINCT ur.Id
            FROM dbo.[User] u
            INNER JOIN UserRole ur ON ur.UserId = u.Id
            WHERE u.UserName = @UserName
    ELSE IF (@RoleId IS NOT NULL)
        INSERT INTO @Ids SELECT DISTINCT Id FROM UserRole WHERE RoleId = @RoleId 
    ELSE IF (@UserId IS NOT NULL)
        INSERT INTO @Ids SELECT DISTINCT Id FROM UserRole WHERE UserId = @UserId 

    SELECT ur.Id, ur.RoleId, ur.UserId, r.Name AS RoleName
    FROM @Ids i
    INNER JOIN UserRole ur ON ur.Id = i.Id
    INNER JOIN [Role] r ON r.Id = ur.RoleId
