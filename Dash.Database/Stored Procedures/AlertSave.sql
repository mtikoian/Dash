CREATE PROCEDURE AlertSave
    @Id INT OUTPUT,
    @Name NVARCHAR(100),
    @ReportId INT,
    @OwnerId INT,
    @SendToEmail NVARCHAR(1000),
    @SendToWebhook NVARCHAR(1000),
    @Subject NVARCHAR(100),
    @ResultCount INT = 0,
    @CronMinute NVARCHAR(5),
    @CronHour NVARCHAR(5),
    @CronDayOfMonth NVARCHAR(5),
    @CronMonth NVARCHAR(5),
    @CronDayOfWeek NVARCHAR(5),
    @LastRunDate DATETIMEOFFSET = NULL,
    @Hash NVARCHAR(100) = NULL,
    @IsActive BIT = 0,
    @NotificationInterval INT = 0,
    @LastNotificationDate DATETIMEOFFSET = NULL,
    @RequestUserId INT = NULL
AS
    SET NOCOUNT ON

    IF ISNULL(@Id, 0) = 0
        BEGIN
            INSERT INTO Alert ([Name], [ReportId], [OwnerId], SendToEmail, SendToWebhook, [Subject], ResultCount, CronMinute, CronHour, CronDayOfMonth, CronMonth, CronDayOfWeek, LastRunDate, [Hash], IsActive, NotificationInterval, LastNotificationDate, UserCreated)
                VALUES (@Name, @ReportId, @OwnerId, @SendToEmail, @SendToWebhook, @Subject, @ResultCount, @CronMinute, @CronHour, @CronDayOfMonth, @CronMonth, @CronDayOfWeek, @LastRunDate, @Hash, @IsActive, @NotificationInterval, @LastNotificationDate, @RequestUserId)
            SET @Id = SCOPE_IDENTITY()
        END
    ELSE
        BEGIN
            UPDATE Alert SET [Name] = @Name, [ReportId] = @ReportId, SendToEmail = @SendToEmail, SendToWebhook = @SendToWebhook, [Subject] = @Subject, ResultCount = @ResultCount,  
                CronMinute = @CronMinute, CronHour = @CronHour, CronDayOfMonth = @CronDayOfMonth, CronMonth = @CronMonth, CronDayOfWeek = @CronDayOfWeek, LastRunDate = ISNULL(@LastRunDate, LastRunDate), 
                [Hash] = @Hash, IsActive = @IsActive, NotificationInterval = @NotificationInterval, LastNotificationDate = ISNULL(@LastNotificationDate, LastNotificationDate),
                UserUpdated = @RequestUserId, DateUpdated = SYSDATETIMEOFFSET()
            WHERE Id = @Id
        END
    RETURN 0
