CREATE PROCEDURE ActivityLogSave
    @Id BIGINT OUTPUT,
    @RequestTimestamp DATETIMEOFFSET = NULL,
    @StatusCode INT = NULL, 
    @Url NVARCHAR(250) = NULL,
    @Method NVARCHAR(10) = NULL,
    @Controller NVARCHAR(100) = NULL,
    @Action NVARCHAR(100) = NULL,
    @RequestData NVARCHAR(4000) = NULL, 
    @Duration BIGINT = NULL,
    @IP NVARCHAR(50) = NULL,
    @RequestUserId INT = NULL
AS
    SET NOCOUNT ON

    SET @RequestTimestamp = ISNULL(@RequestTimestamp, SYSDATETIMEOFFSET())
    INSERT INTO ActivityLog (RequestTimestamp, StatusCode, [Url], Method, Controller, [Action], IP, [UserId], RequestData, Duration)
        VALUES (@RequestTimestamp, @StatusCode, @Url, @Method, @Controller, @Action, @IP, @RequestUserId, @RequestData, @Duration)