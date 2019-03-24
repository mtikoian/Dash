CREATE PROCEDURE UserRoleDelete
    @Id INT,
    @RequestUserId INT = NULL
AS
    SET NOCOUNT ON

    DELETE FROM UserRole WHERE Id = @Id
    RETURN 0
