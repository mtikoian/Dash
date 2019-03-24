CREATE PROCEDURE RolePermissionGet
    @Id INT = NULL,
    @RoleId INT = NULL
AS
    SET NOCOUNT ON

    DECLARE @Ids TABLE (Id INT NOT NULL PRIMARY KEY)
    IF (@Id IS NOT NULL)
        INSERT INTO @Ids SELECT @Id
    ELSE IF (@RoleId IS NOT NULL)
        INSERT INTO @Ids SELECT Id FROM RolePermission WHERE RoleId = @RoleId

    SELECT rp.Id, RoleId, PermissionId
    FROM @Ids i
    INNER JOIN RolePermission rp ON rp.Id = i.Id
