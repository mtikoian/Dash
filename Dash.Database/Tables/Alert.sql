CREATE TABLE [dbo].[Alert] (
    [Id]                   INT                IDENTITY (1, 1) NOT NULL,
    [Name]                 NVARCHAR (100)     NOT NULL,
    [ReportId]             INT                NOT NULL,
    [SendToEmail]          NVARCHAR (1000)    NULL,
    [SendToWebhook]        NVARCHAR (1000)    NULL,
    [Subject]              NVARCHAR (100)     NOT NULL,
    [ResultCount]          INT                NOT NULL DEFAULT 1,
    [CronMinute]           NVARCHAR (5)       NOT NULL,
    [CronHour]             NVARCHAR (5)       NOT NULL,
    [CronDayOfMonth]       NVARCHAR (5)    NOT NULL,
    [CronMonth]            NVARCHAR (5)       NOT NULL,
    [CronDayOfWeek]        NVARCHAR (5)     NOT NULL,
    [LastRunDate]          DATETIMEOFFSET (7) NULL,
    [Hash]                 NVARCHAR (100)     NULL,
    [NotificationInterval] INT       NOT NULL DEFAULT 0,
    [LastNotificationDate] DATETIMEOFFSET (7) NULL,
    [IsActive]             BIT                NOT NULL DEFAULT 1,
    [DateCreated]          DATETIMEOFFSET (7) DEFAULT (sysdatetimeoffset()) NOT NULL,
    [UserCreated]          INT                NULL,
    [DateUpdated]          DATETIMEOFFSET (7) NULL,
    [UserUpdated]          INT                NULL,
    CONSTRAINT [PK_Alert] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Alert_UserCreated] FOREIGN KEY ([UserCreated]) REFERENCES [dbo].[User] ([Id]),
    CONSTRAINT [FK_Alert_UserUpdated] FOREIGN KEY ([UserUpdated]) REFERENCES [dbo].[User] ([Id])
);
GO

CREATE NONCLUSTERED INDEX [IX_Alert_UserCreated]
    ON [dbo].[Alert]([UserCreated] ASC);
GO
