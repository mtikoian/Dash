CREATE PROCEDURE RolePermissionSave
    @Id INT OUTPUT,
    @RoleId INT,
    @PermissionId INT,
    @RequestUserId INT = NULL
 AS
    SET NOCOUNT ON

    IF ISNULL(@Id, 0) = 0
        BEGIN
            INSERT INTO RolePermission (RoleId, PermissionId, UserCreated) VALUES (@RoleId, @PermissionId, @RequestUserId)
            SET @Id = SCOPE_IDENTITY()
        END
    ELSE
        BEGIN
            UPDATE RolePermission SET PermissionId = @PermissionId, RoleId = @RoleId, UserUpdated = @RequestUserId, DateUpdated = SYSDATETIMEOFFSET() WHERE Id = @Id
        END
    RETURN 0
