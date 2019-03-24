CREATE PROCEDURE WidgetGet
    @Id INT = NULL,
    @UserId INT = NULL
AS
    SET NOCOUNT ON

    DECLARE @Ids TABLE (Id INT NOT NULL PRIMARY KEY)
    IF (@Id IS NOT NULL)
        INSERT INTO @Ids SELECT @Id
    ELSE IF (@UserId IS NOT NULL)
        INSERT INTO @Ids SELECT Id FROM Widget WHERE UserCreated = @UserId

    SELECT w.Id, w.UserCreated, w.Title, w.ReportId, w.ChartId, w.RefreshRate, w.X, w.Y, w.Width, w.Height,
        r.RowLimit AS ReportRowLimit, r.Width AS ReportWidth, r.DatasetId, d.[DateFormat] AS DisplayDateFormat, d.CurrencyFormat AS DisplayCurrencyFormat
    FROM @Ids i
    INNER JOIN Widget w ON w.Id = i.Id
    LEFT JOIN Report r ON r.Id = w.ReportId
    LEFT JOIN Dataset d ON d.Id = r.DatasetId
    ORDER BY X, Y

