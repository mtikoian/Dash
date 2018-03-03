CREATE TABLE [dbo].[ActivityLog] (
    [Id]               BIGINT             IDENTITY (1, 1) NOT NULL,
    [RequestTimestamp] DATETIMEOFFSET (7) DEFAULT (getdate()) NOT NULL,
    [StatusCode]       INT                NULL,
    [Url]              NVARCHAR (250)     NOT NULL,
    [Method]           NVARCHAR (10)      NOT NULL,
    [Controller]       NVARCHAR (100)     NULL,
    [Action]           NVARCHAR (100)     NULL,
    [IP]               NVARCHAR (50)      NULL,
    [UserId]           INT                NULL,
    [RequestData]      NVARCHAR (4000)    NULL,
    [Duration]         BIGINT             NULL,
    CONSTRAINT [PK_ActivityLog] PRIMARY KEY CLUSTERED ([Id] ASC)
);

