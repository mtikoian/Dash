CREATE PROCEDURE UserLoginSave
	@Id INT,
	@LoginHash NVARCHAR(50) = NULL,
	@DateLogin DATETIMEOFFSET = NULL,
	@RequestUserId INT = NULL
 AS
	SET NOCOUNT ON

	UPDATE [User] SET LoginHash = @LoginHash, DateLogin = @DateLogin, UserUpdated = @RequestUserId, DateUpdated = SYSDATETIMEOFFSET() WHERE Id = @Id
	RETURN 0
