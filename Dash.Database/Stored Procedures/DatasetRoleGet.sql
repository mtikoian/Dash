CREATE PROCEDURE DatasetRoleGet
	@Id INT = NULL,
	@RoleId INT = NULL,
	@DatasetId INT = NULL
AS
	SET NOCOUNT ON
	
	DECLARE @Ids TABLE (Id INT NOT NULL PRIMARY KEY)
	IF (@Id IS NOT NULL)
		INSERT INTO @Ids SELECT @Id
	ELSE IF (@RoleId IS NOT NULL AND @DatasetId IS NOT NULL)
		INSERT INTO @Ids SELECT Id FROM DatasetRole WHERE RoleId = @RoleId AND DatasetId = @DatasetId
	ELSE IF (@RoleId IS NOT NULL)
		INSERT INTO @Ids SELECT Id FROM DatasetRole WHERE RoleId = @RoleId
	ELSE IF (@DatasetId IS NOT NULL)
		INSERT INTO @Ids SELECT Id FROM DatasetRole WHERE DatasetId = @DatasetId

	SELECT dr.Id, DatasetId, RoleId 
	FROM @Ids i
	INNER JOIN DatasetRole dr ON dr.Id = i.Id