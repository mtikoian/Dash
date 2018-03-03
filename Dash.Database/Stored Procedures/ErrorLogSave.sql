CREATE PROCEDURE ErrorLogSave
    @Id BIGINT OUTPUT,
    @Namespace NVARCHAR(100),
    @Host NVARCHAR(100),
    @Type NVARCHAR(100),
    @Source NVARCHAR(100),
    @Path NVARCHAR(100),
    @Method NVARCHAR(100),
    @Message NVARCHAR(500),
    @User NVARCHAR(50),
    @StackTrace NTEXT,
    @Timestamp DATETIMEOFFSET,
    @RequestUserId INT = NULL
AS
    SET NOCOUNT ON

    INSERT INTO ErrorLog ([Namespace], [Host], [Type], [Source], [Path], [Method], [Message], [User], [StackTrace], [Timestamp] )
        VALUES (@Namespace, @Host, @Type, @Source, @Path, @Method, @Message, @User, @StackTrace, @Timestamp)