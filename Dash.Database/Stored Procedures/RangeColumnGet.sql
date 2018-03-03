CREATE PROCEDURE RangeColumnGet
	@UserId INT
AS
	SET NOCOUNT ON
	
	DECLARE @Ids TABLE (Id INT NOT NULL PRIMARY KEY)
	INSERT INTO @Ids SELECT Id FROM Report WHERE OwnerId = @UserId
	INSERT INTO @Ids SELECT DISTINCT ReportId FROM ReportShare rs WHERE rs.UserId = @UserId AND ReportId NOT IN (SELECT Id FROM @Ids)
	INSERT INTO @Ids SELECT DISTINCT ReportId FROM ReportShare rs INNER JOIN UserRole ur ON ur.RoleId = rs.RoleId WHERE ur.UserId = @UserId AND ReportId NOT IN (SELECT Id FROM @Ids)
	INSERT INTO @Ids SELECT DISTINCT ReportId FROM ChartRange cr INNER JOIN ChartShare cs ON cs.ChartId = cr.ChartId WHERE @UserId = cs.UserId AND ReportId NOT IN (SELECT Id FROM @Ids)
	INSERT INTO @Ids SELECT DISTINCT ReportId FROM ChartRange cr INNER JOIN ChartShare cs ON cs.ChartId = cr.ChartId
		INNER JOIN UserRole ur ON ur.RoleId = cs.RoleId WHERE ur.UserId = @UserId AND ReportId NOT IN (SELECT Id FROM @Ids)

	SELECT i.Id AS ReportId, dc.Id AS ColumnId, dc.Title, dc.FilterTypeId
	FROM @Ids i
	INNER JOIN Report r ON r.Id = i.Id
	INNER JOIN DatasetColumn dc ON dc.DatasetId = r.DatasetId
	ORDER BY i.Id, dc.Title