CREATE PROCEDURE DatasetColumnGet
	@Id INT = NULL,
	@DatasetId INT = NULL
AS
	SET NOCOUNT ON
	
	DECLARE @Ids TABLE (Id INT NOT NULL PRIMARY KEY)
	IF (@Id IS NOT NULL)
		INSERT INTO @Ids SELECT @Id
	ELSE IF (@DatasetId IS NOT NULL)
		INSERT INTO @Ids SELECT Id FROM DatasetColumn WHERE DatasetId = @DatasetId

	SELECT dc.Id, DatasetId, Title, ColumnName, Derived, FilterTypeId, FilterQuery, DataTypeId, Link, IsParam
	FROM @Ids i
	INNER JOIN DatasetColumn dc ON dc.Id = i.Id
	ORDER BY Title