CREATE PROCEDURE UserHasDatasetAccess
	@UserId INT,
	@DatasetId INT
AS
	SET NOCOUNT ON
	
	SELECT TOP 1 ur.UserId 
		FROM UserRole ur 
		INNER JOIN DatasetRole dr ON dr.RoleId = ur.RoleId
		WHERE ur.UserId = @UserId AND dr.DatasetId = @DatasetId