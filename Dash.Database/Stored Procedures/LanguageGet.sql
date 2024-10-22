﻿CREATE PROCEDURE LanguageGet
    @Id INT = NULL
AS
    SET NOCOUNT ON

    DECLARE @Ids TABLE (Id INT NOT NULL PRIMARY KEY)
    IF (@Id IS NOT NULL)
        INSERT INTO @Ids SELECT @Id
    ELSE
        INSERT INTO @Ids SELECT Id FROM [Language]

    SELECT l.Id, Name, LanguageCode, CountryCode
    FROM @Ids i
    INNER JOIN [Language] l ON l.Id = i.Id
    ORDER BY Name
