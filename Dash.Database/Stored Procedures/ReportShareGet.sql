CREATE PROCEDURE ReportShareGet
	@Id INT = NULL,
	@ReportId INT = NULL
AS
	SET NOCOUNT ON
	
	DECLARE @Ids TABLE (Id INT NOT NULL PRIMARY KEY)
	IF (@Id IS NOT NULL)
		INSERT INTO @Ids SELECT @Id
	ELSE IF (@ReportId IS NOT NULL)
		INSERT INTO @Ids SELECT Id FROM ReportShare WHERE ReportId = @ReportId

	SELECT rs.Id, ReportId, UserId, RoleId 
	FROM @Ids i
	INNER JOIN ReportShare rs ON rs.Id = i.Id