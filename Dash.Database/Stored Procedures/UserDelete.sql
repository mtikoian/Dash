CREATE PROCEDURE UserDelete
	@Id INT,
	@RequestUserId INT = NULL
AS
	SET NOCOUNT ON

	UPDATE [User] SET IsDeleted = 1, UserUpdated = @RequestUserId, DateUpdated = SYSDATETIMEOFFSET() WHERE Id = @Id
	RETURN 0