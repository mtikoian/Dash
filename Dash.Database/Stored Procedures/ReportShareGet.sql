CREATE PROCEDURE ReportShareGet
    @Id INT = NULL,
    @ReportId INT = NULL
AS
    SET NOCOUNT ON
    
    DECLARE @Ids TABLE (Id INT NOT NULL PRIMARY KEY)
    IF (@Id IS NOT NULL)
        INSERT INTO @Ids SELECT @Id
    ELSE IF (@ReportId IS NOT NULL)
        INSERT INTO @Ids SELECT Id FROM ReportShare WHERE ReportId = @ReportId

    SELECT rs.Id, rs.ReportId, rs.UserId, rs.RoleId, r.[Name] AS RoleName, u.FirstName AS UserFirstName, u.LastName AS UserLastName
    FROM @Ids i
    INNER JOIN ReportShare rs ON rs.Id = i.Id
    LEFT JOIN [Role] r ON r.Id = rs.RoleId
    LEFT JOIN [User] u ON u.Id = rs.UserId
