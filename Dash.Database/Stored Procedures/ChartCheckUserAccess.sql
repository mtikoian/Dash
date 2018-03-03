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

    SELECT @MatchedUser