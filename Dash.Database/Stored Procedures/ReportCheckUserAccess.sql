CREATE PROCEDURE ReportCheckUserAccess
    @ReportId INT,
    @UserId INT
 AS
    SET NOCOUNT ON
    
    DECLARE @MatchedUser BIT = 0

    IF EXISTS (SELECT 1 FROM ReportShare WHERE ReportId = @ReportId AND UserId = @UserId)
        SET @MatchedUser = 1
    ELSE IF EXISTS (SELECT 1 FROM ReportShare INNER JOIN UserRole on UserRole.RoleId = ReportShare.RoleId WHERE ReportId = @ReportId AND UserRole.UserId = @UserId)
        SET @MatchedUser = 1
    ELSE IF EXISTS (SELECT 1 FROM ChartShare cs INNER JOIN ChartRange cr ON cs.ChartId = cr.ChartId WHERE cr.ReportId = @ReportId AND cs.UserId = @UserId)
        SET @MatchedUser = 1
    ELSE IF EXISTS (SELECT 1 FROM ChartShare cs INNER JOIN ChartRange cr ON cs.ChartId = cr.ChartId INNER JOIN UserRole ur on ur.RoleId = cs.RoleId WHERE cr.ReportId = @ReportId AND ur.UserId = @UserId)
        SET @MatchedUser = 1


    SELECT @MatchedUser