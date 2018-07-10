CREATE PROCEDURE AlertGetActive
AS
	SET NOCOUNT ON
	
	SELECT Id, [Name], ReportId, OwnerId, DateCreated, ISNULL(DateUpdated, DateCreated) AS DateUpdated, IsActive,
        SendTo, [Subject], ResultCount, CronMinute, CronHour, CronDayOfMonth, CronMonth, CronDayOfWeek, LastRunDate, [Hash], NotificationInterval, LastNotificationDate
	FROM Alert
    WHERE IsActive = 1
	ORDER BY [Name]
