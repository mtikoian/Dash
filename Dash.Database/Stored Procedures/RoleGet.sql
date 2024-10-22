﻿CREATE PROCEDURE RoleGet
    @Id INT = NULL,
    @Name NVARCHAR(100) = NULL
AS
    SET NOCOUNT ON

    DECLARE @Ids TABLE (Id INT NOT NULL PRIMARY KEY)
    IF (@Id IS NOT NULL)
        INSERT INTO @Ids SELECT @Id
    ELSE IF (@Name IS NOT NULL)
        INSERT INTO @Ids SELECT Id FROM [Role] WHERE [Name] = @Name
    ELSE
        INSERT INTO @Ids SELECT Id FROM [Role]

    SELECT r.Id, [Name]
    FROM @Ids i
    INNER JOIN [Role] r ON r.Id = i.Id
    ORDER BY [Name]
