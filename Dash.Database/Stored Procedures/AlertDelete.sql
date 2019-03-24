CREATE PROCEDURE AlertDelete
    @Id INT,
    @RequestUserId INT = NULL
AS
    SET NOCOUNT ON

    DELETE FROM Alert WHERE Id = @Id
