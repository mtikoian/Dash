CREATE PROCEDURE DatasetJoinGet
    @Id INT = NULL,
    @DatasetId INT = NULL
AS
    SET NOCOUNT ON

    DECLARE @Ids TABLE (Id INT NOT NULL PRIMARY KEY)
    IF (@Id IS NOT NULL)
        INSERT INTO @Ids SELECT @Id
    ELSE IF (@DatasetId IS NOT NULL)
        INSERT INTO @Ids SELECT Id FROM DatasetJoin WHERE DatasetId = @DatasetId

    SELECT dj.Id, DatasetId, TableName, JoinTypeId, Keys, JoinOrder
    FROM @Ids i
    INNER JOIN DatasetJoin dj ON dj.Id = i.Id
    ORDER BY JoinOrder
