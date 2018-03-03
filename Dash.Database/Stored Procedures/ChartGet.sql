CREATE PROCEDURE ChartGet
	@Id INT = NULL,
	@UserId INT = NULL
AS
	SET NOCOUNT ON
	
	DECLARE @Ids TABLE (Id INT NOT NULL PRIMARY KEY)
	IF (@Id IS NOT NULL)
		INSERT INTO @Ids SELECT @Id
	ELSE IF (@UserId IS NOT NULL)
		BEGIN
			-- there may be a more efficient way to do this. i'm opting for multiple queries instead of a complex predicate
			INSERT INTO @Ids SELECT Id FROM Chart WHERE OwnerId = @UserId
			INSERT INTO @Ids SELECT DISTINCT ChartId FROM ChartShare cs 
				INNER JOIN UserRole ur ON ur.RoleId = cs.RoleId 
				WHERE @UserId IN (ur.UserId, cs.UserId) AND ChartId NOT IN (SELECT Id FROM @Ids)
		END
	ELSE
		INSERT INTO @Ids SELECT Id FROM Chart

	SELECT c.Id, c.Name, c.ChartTypeId, c.OwnerId, c.DateCreated, ISNULL(c.DateUpdated, c.DateCreated) AS DateUpdated
	FROM @Ids i
	INNER JOIN Chart c ON c.Id = i.Id
	ORDER BY c.Name