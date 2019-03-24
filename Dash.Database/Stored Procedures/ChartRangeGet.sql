CREATE PROCEDURE ChartRangeGet
    @Id INT = NULL,
    @ChartId INT = NULL
AS
    SET NOCOUNT ON

    DECLARE @Ids TABLE (Id INT NOT NULL PRIMARY KEY)
    IF (@Id IS NOT NULL)
        INSERT INTO @Ids SELECT @Id
    ELSE IF (@ChartId IS NOT NULL)
        INSERT INTO @Ids SELECT Id FROM ChartRange WHERE ChartId = @ChartId

    SELECT cr.Id, ChartId, ReportId, XAxisColumnId, YAxisColumnId, AggregatorId, DateIntervalId, FillDateGaps, DisplayOrder, Color
    FROM @Ids i
    INNER JOIN ChartRange cr ON cr.Id = i.Id
    ORDER BY cr.DisplayOrder
