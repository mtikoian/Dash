
CREATE PROCEDURE UserResetSave
	@Id INT,
	@ResetHash NVARCHAR(500) = NULL,
	@DateReset DATETIMEOFFSET = NULL,
	@RequestUserId INT = NULL
 AS
	SET NOCOUNT ON

	UPDATE [User] SET ResetHash = @ResetHash, DateReset = @DateReset, UserUpdated = @RequestUserId, DateUpdated = SYSDATETIMEOFFSET() WHERE Id = @Id
	RETURN 0
