CREATE PROCEDURE UserHasDatasetAccess
    @UserId INT,
    @DatasetId INT
AS
    SET NOCOUNT ON

    SELECT TOP 1 @UserId
        FROM Dataset d
        LEFT JOIN DatasetRole dr ON dr.DatasetId = d.Id
        LEFT JOIN UserRole ur ON dr.RoleId = ur.RoleId
        WHERE d.Id = @DatasetId AND (ur.UserId = @UserId OR d.UserCreated = @UserId)
