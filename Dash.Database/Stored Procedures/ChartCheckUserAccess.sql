CREATE PROCEDURE ChartCheckUserAccess
    @ChartId INT,
    @UserId INT
 AS
    SET NOCOUNT ON
    
    DECLARE @MatchedUser BIT = 0

    IF EXISTS (SELECT 1 FROM ChartShare WHERE ChartId = @ChartId AND UserId = @UserId)
        SET @MatchedUser = 1
    ELSE IF EXISTS (SELECT 1 FROM ChartShare INNER JOIN UserRole on UserRole.RoleId = ChartShare.RoleId WHERE ChartId = @ChartId AND UserRole.UserId = @UserId)
        SET @MatchedUser = 1

    DECLARE @DatasetIds TABLE (DatasetId INT NOT NULL PRIMARY KEY, Allowed BIT NOT NULL DEFAULT 0)
    INSERT INTO @DatasetIds SELECT DatasetId FROM ChartRange cr INNER JOIN Report r ON r.Id = cr.ReportId WHERE cr.ChartId = @ChartId

    -- @todo this requires more testing
    IF EXISTS (SELECT 1
        FROM ChartRange cr 
        INNER JOIN Report r ON r.Id = cr.ReportId 
        INNER JOIN DatasetRole dr ON dr.DatasetId = r.DatasetId
        LEFT JOIN UserRole ur ON dr.RoleId = ur.RoleId AND ur.UserId = @UserId
        WHERE cr.ChartId = @ChartId AND ur.UserId IS NULL)
    BEGIN
        SET @MatchedUser = 0
    END

    SELECT @MatchedUser