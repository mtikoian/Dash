CREATE PROCEDURE PermissionGet
    @Id INT = NULL,
    @UserId INT = NULL
 AS
    SET NOCOUNT ON

    DECLARE @Ids TABLE (Id INT NOT NULL PRIMARY KEY)
    IF (@Id IS NOT NULL)
        INSERT INTO @Ids SELECT @Id
    ELSE IF (@UserId IS NOT NULL)
        INSERT INTO @Ids SELECT DISTINCT p.Id
            FROM UserRole ur
            INNER JOIN RolePermission rp ON rp.RoleId = ur.RoleId
            INNER JOIN Permission p ON p.Id = rp.PermissionId
            WHERE ur.UserId = @UserId
    ELSE
        INSERT INTO @Ids SELECT Id FROM Permission

    SELECT p.Id, p.ControllerName, p.ActionName
    FROM @Ids i
    INNER JOIN Permission p ON p.Id = i.Id
    ORDER BY p.ControllerName, p.ActionName
