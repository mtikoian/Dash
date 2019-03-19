CREATE PROCEDURE ReportGet
	@Id INT = NULL,
	@UserId INT = NULL
AS
	SET NOCOUNT ON
	
	DECLARE @Ids TABLE (Id INT NOT NULL PRIMARY KEY)
	IF (@Id IS NOT NULL)
		INSERT INTO @Ids SELECT @Id
	ELSE IF (@UserId IS NOT NULL)
		BEGIN
			INSERT INTO @Ids SELECT Id FROM Report WHERE UserCreated = @UserId
			INSERT INTO @Ids SELECT DISTINCT ReportId FROM ReportShare rs WHERE rs.UserId = @UserId AND ReportId NOT IN (SELECT Id FROM @Ids)
			INSERT INTO @Ids SELECT DISTINCT ReportId FROM ReportShare rs INNER JOIN UserRole ur ON ur.RoleId = rs.RoleId WHERE ur.UserId = @UserId AND ReportId NOT IN (SELECT Id FROM @Ids)
			INSERT INTO @Ids SELECT DISTINCT ReportId FROM ChartRange cr INNER JOIN ChartShare cs ON cs.ChartId = cr.ChartId WHERE @UserId = cs.UserId AND ReportId NOT IN (SELECT Id FROM @Ids)
			INSERT INTO @Ids SELECT DISTINCT ReportId FROM ChartRange cr INNER JOIN ChartShare cs ON cs.ChartId = cr.ChartId
				INNER JOIN UserRole ur ON ur.RoleId = cs.RoleId WHERE ur.UserId = @UserId AND ReportId NOT IN (SELECT Id FROM @Ids)
		END
	ELSE
		INSERT INTO @Ids SELECT Id FROM Report

	SELECT r.Id, r.DatasetId, r.Name, r.RowLimit, r.XAxisColumnId, r.ChartDateIntervalId,
		r.YAxisColumnId, r.ChartAggregatorId, r.ChartTypeId, r.UserCreated, r.Width, r.AggregatorId, d.Name AS DatasetName, d.DatabaseId, r.DateCreated, ISNULL(r.DateUpdated, r.DateCreated) AS DateUpdated
	FROM @Ids i
	INNER JOIN Report r ON r.Id = i.Id
	INNER JOIN Dataset d ON d.Id = r.DatasetId 
	ORDER BY r.Name
