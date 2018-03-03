CREATE PROCEDURE WidgetGet
	@Id INT = NULL,
	@UserId INT = NULL
AS
	SET NOCOUNT ON
	
	DECLARE @Ids TABLE (Id INT NOT NULL PRIMARY KEY)
	IF (@Id IS NOT NULL)
		INSERT INTO @Ids SELECT @Id
	ELSE IF (@UserId IS NOT NULL)
		INSERT INTO @Ids SELECT Id FROM Widget WHERE UserId = @UserId

	DECLARE @results TABLE (Id INT NOT NULL PRIMARY KEY, ReportRowLimit INT NULL, ReportWidth DECIMAL(18,14) NULL, DatasetId INT NULL, DisplayDateFormat NVARCHAR(50) NULL, 
		DisplayCurrencyFormat NVARCHAR(50) NULL, WidgetDateUpdated DATETIMEOFFSET NULL)

	INSERT INTO @results (Id, WidgetDateUpdated)
		SELECT w.Id, (SELECT MAX(v) FROM (VALUES (w.DateUpdated), (c.DateUpdated), (x.RangeDateUpdated)) AS VALUE(v))
		FROM @Ids i
		INNER JOIN Widget w ON w.Id = i.Id
		INNER JOIN Chart c ON c.Id = w.ChartId
		OUTER APPLY (
			SELECT TOP 1 (SELECT MAX(v) FROM (VALUES (cr.DateUpdated), (r.DateUpdated), (d.DateUpdated), (db.DateUpdated)) AS VALUE(v)) AS RangeDateUpdated, cr.Id
			FROM ChartRange cr
			INNER JOIN Report r ON r.Id = cr.ReportId
			INNER JOIN Dataset d ON d.Id = r.DatasetId
			INNER JOIN [Database] db ON db.Id = d.DatabaseId
			WHERE cr.ChartId = w.ChartId
			ORDER BY RangeDateUpdated
		) x

	INSERT INTO @results 
		SELECT DISTINCT w.Id, r.RowLimit, r.Width, r.DatasetId, d.[DateFormat], d.CurrencyFormat, 
			(SELECT MAX(v) FROM (VALUES (w.DateUpdated), (r.DateUpdated), (d.DateUpdated), (db.DateUpdated)) AS VALUE(v))
		FROM @Ids i
		INNER JOIN Widget w ON w.Id = i.Id
		INNER JOIN Report r ON r.Id = w.ReportId
		INNER JOIN Dataset d ON d.Id = r.DatasetId
		INNER JOIN [Database] db ON db.Id = d.DatabaseId

	SELECT w.Id, w.UserId, w.ReportId, w.ChartId, w.RefreshRate, w.X, w.Y, w.Width, w.Height, w.Title,
		r.ReportRowLimit, r.ReportWidth, r.DatasetId, r.DisplayDateFormat, r.DisplayCurrencyFormat, WidgetDateUpdated
	FROM @results r
	INNER JOIN Widget w ON w.Id = r.Id
	ORDER BY X, Y

