CREATE PROCEDURE ReportColumnGet
    @Id INT = NULL,
    @ReportId INT = NULL
AS
    SET NOCOUNT ON

    DECLARE @Ids TABLE (Id INT NOT NULL PRIMARY KEY)
    IF (@Id IS NOT NULL)
        INSERT INTO @Ids SELECT @Id
    ELSE IF (@ReportId IS NOT NULL)
        INSERT INTO @Ids SELECT Id FROM ReportColumn WHERE ReportId = @ReportId

    SELECT rc.Id, ReportId, ColumnId, DisplayOrder, Width, SortOrder, SortDirection
    FROM @Ids i
    INNER JOIN ReportColumn rc ON rc.Id = i.Id
    ORDER BY DisplayOrder
