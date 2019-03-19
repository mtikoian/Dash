CREATE PROCEDURE AlertGet
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
			INSERT INTO @Ids SELECT Id FROM Alert WHERE UserCreated = @UserId
		END
	ELSE
		INSERT INTO @Ids SELECT Id FROM Alert

	SELECT a.Id, a.[Name], a.ReportId, a.UserCreated, a.DateCreated, ISNULL(a.DateUpdated, a.DateCreated) AS DateUpdated, IsActive,
        SendToEmail, SendToWebhook, [Subject], ResultCount, CronMinute, CronHour, CronDayOfMonth, CronMonth, CronDayOfWeek, LastRunDate, [Hash], NotificationInterval, LastNotificationDate
	FROM @Ids i
	INNER JOIN Alert a ON a.Id = i.Id
	ORDER BY a.[Name]
