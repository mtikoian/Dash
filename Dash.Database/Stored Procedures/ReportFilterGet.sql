CREATE PROCEDURE ReportFilterGet
	@Id INT = NULL,
	@ReportId INT = NULL
AS
	SET NOCOUNT ON
	
	DECLARE @Ids TABLE (Id INT NOT NULL PRIMARY KEY)
	IF (@Id IS NOT NULL)
		INSERT INTO @Ids SELECT @Id
	ELSE IF (@ReportId IS NOT NULL)
		INSERT INTO @Ids SELECT Id FROM ReportFilter WHERE ReportId = @ReportId

	SELECT rf.Id, ReportId, ColumnId, DisplayOrder, OperatorId, Criteria, Criteria2 
	FROM @Ids i
	INNER JOIN ReportFilter rf ON rf.Id = i.Id