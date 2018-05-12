CREATE PROCEDURE UserLoginSave
	@Id INT,
	@LoginHash NVARCHAR(50) = NULL,
	@DateLoginWindow DATETIMEOFFSET = NULL,
	@RequestUserId INT = NULL
 AS
	SET NOCOUNT ON

	UPDATE [User] SET LoginHash = @LoginHash, DateLoginWindow = @DateLoginWindow, UserUpdated = @RequestUserId, DateUpdated = SYSDATETIMEOFFSET() WHERE Id = @Id
	RETURN 0
