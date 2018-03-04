CREATE PROCEDURE RolePermissionDelete
	@Id INT,
	@RequestUserId INT = NULL
AS
	SET NOCOUNT ON

	DELETE FROM RolePermission WHERE Id = @Id
	RETURN 0
