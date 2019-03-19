CREATE PROCEDURE AlertGetActive
AS
	SET NOCOUNT ON
	
	SELECT Id, [Name], ReportId, UserCreated, DateCreated, ISNULL(DateUpdated, DateCreated) AS DateUpdated, IsActive,
        SendToEmail, SendToWebhook, [Subject], ResultCount, CronMinute, CronHour, CronDayOfMonth, CronMonth, CronDayOfWeek, LastRunDate, [Hash], NotificationInterval, LastNotificationDate
	FROM Alert
    WHERE IsActive = 1
	ORDER BY [Name]
