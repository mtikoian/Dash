CREATE PROCEDURE ReportGroupGet
	@Id INT = NULL,
	@ReportId INT = NULL
AS
	SET NOCOUNT ON
	
	DECLARE @Ids TABLE (Id INT NOT NULL PRIMARY KEY)
	IF (@Id IS NOT NULL)
		INSERT INTO @Ids SELECT @Id
	ELSE IF (@ReportId IS NOT NULL)
		INSERT INTO @Ids SELECT Id FROM ReportGroup WHERE ReportId = @ReportId

	SELECT rg.Id, ReportId, ColumnId, DisplayOrder 
	FROM @Ids i
	INNER JOIN ReportGroup rg ON rg.Id = i.Id
	ORDER BY DisplayOrder