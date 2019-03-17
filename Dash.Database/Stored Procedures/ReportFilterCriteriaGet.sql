CREATE PROCEDURE ReportFilterCriteriaGet
	@Id INT = NULL,
	@ReportFilterId INT = NULL
AS
	SET NOCOUNT ON
	
	DECLARE @Ids TABLE (Id INT NOT NULL PRIMARY KEY)
	IF (@Id IS NOT NULL)
		INSERT INTO @Ids SELECT @Id
	ELSE IF (@ReportFilterId IS NOT NULL)
		INSERT INTO @Ids SELECT Id FROM ReportFilterCriteria WHERE ReportFilterId = @ReportFilterId

	SELECT rf.Id, ReportFilterId, [Value]
	FROM @Ids i
	INNER JOIN ReportFilterCriteria rf ON rf.Id = i.Id
