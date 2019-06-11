CREATE PROCEDURE DatasetGet
    @Id INT = NULL,
    @UserId INT = NULL,
    @Name NVARCHAR(100) = NULL
AS
    SET NOCOUNT ON

    DECLARE @Ids TABLE (Id INT NOT NULL PRIMARY KEY)
    IF (@Id IS NOT NULL)
        INSERT INTO @Ids SELECT @Id
    ELSE IF (@UserId IS NOT NULL)
        INSERT INTO @Ids SELECT DISTINCT d.Id FROM Dataset d
            LEFT JOIN DatasetRole dr ON d.Id = dr.DatasetId
            LEFT JOIN UserRole ur ON ur.RoleId = dr.RoleId
            WHERE ur.UserId = @UserId OR d.UserCreated = @UserId
    ELSE IF (@Name IS NOT NULL)
        INSERT INTO @Ids SELECT Id FROM Dataset WHERE Name = @Name
    ELSE
        INSERT INTO @Ids SELECT Id FROM Dataset

    SELECT ds.Id, ds.[Name], ds.PrimarySource, ds.TypeId, ds.Conditions,
        ds.DatabaseId, ds.[DateFormat], ds.TimeFormat, ds.CurrencyFormat, [Database].[Name] AS DatabaseName, [Database].Host AS DatabaseHost
    FROM @Ids i
    INNER JOIN Dataset ds ON ds.Id = i.Id
    LEFT JOIN [Database] ON [Database].Id = ds.DatabaseId
    ORDER BY ds.Name
