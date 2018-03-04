CREATE PROCEDURE UserClaimGet
	@Id INT
 AS
	SET NOCOUNT ON
	
	SELECT p.ControllerName, p.ActionName
		FROM [User] u
		INNER JOIN UserRole ur ON ur.UserId = u.Id
		INNER JOIN RolePermission rp ON rp.RoleId = ur.RoleId
		INNER JOIN Permission p ON p.Id = rp.PermissionId
		WHERE u.Id = @Id AND u.[Status] = 1 AND u.IsActive = 1
