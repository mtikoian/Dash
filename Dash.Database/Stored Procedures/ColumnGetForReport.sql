CREATE PROCEDURE ColumnGetForReport
    @ReportId INT
AS
    SET NOCOUNT ON

    SELECT r.Id, Title, ColumnId, DataTypeId, Width, DisplayOrder, SortDirection, SortOrder, d.DataTypeId, d.Link
    FROM DatasetColumn d
    INNER JOIN ReportColumn r ON r.ColumnId=d.Id
    WHERE r.ReportId = @ReportId
    ORDER BY DisplayOrder
