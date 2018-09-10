CREATE PROCEDURE ChartShareGet
    @Id INT = NULL,
    @ChartId INT = NULL
AS
    SET NOCOUNT ON
    
    DECLARE @Ids TABLE (Id INT NOT NULL PRIMARY KEY)
    IF (@Id IS NOT NULL)
        INSERT INTO @Ids SELECT @Id
    ELSE IF (@ChartId IS NOT NULL)
        INSERT INTO @Ids SELECT Id FROM ChartShare WHERE ChartId = @ChartId

    SELECT cs.Id, cs.ChartId, cs.UserId, cs.RoleId, r.[Name] AS RoleName, u.FirstName AS UserFirstName, u.LastName AS UserLastName
    FROM @Ids i
    INNER JOIN ChartShare cs ON cs.Id = i.Id
    LEFT JOIN [Role] r ON r.Id = cs.RoleId
    LEFT JOIN [User] u ON u.Id = cs.UserId
