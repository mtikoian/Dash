CREATE PROCEDURE DataTypeGet
    @Id INT = NULL
AS
    SET NOCOUNT ON

    DECLARE @Ids TABLE (Id INT NOT NULL PRIMARY KEY)
    IF (@Id IS NOT NULL)
        INSERT INTO @Ids SELECT @Id
    ELSE
        INSERT INTO @Ids SELECT Id FROM DataType

    SELECT dt.Id, Name, IsCurrency, IsDateTime, IsDecimal, IsInteger, IsText, IsBool, IsBinary
    FROM @Ids i
    INNER JOIN DataType dt ON dt.Id = i.Id
    ORDER BY Name
