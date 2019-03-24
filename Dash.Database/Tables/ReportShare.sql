CREATE TABLE [dbo].[ReportShare] (
    [Id]          INT                IDENTITY (1, 1) NOT NULL,
    [ReportId]    INT                NOT NULL,
    [UserId]      INT                NULL,
    [RoleId]      INT                NULL,
    [DateCreated] DATETIMEOFFSET (7) DEFAULT (sysdatetimeoffset()) NOT NULL,
    [UserCreated] INT                NULL,
    [DateUpdated] DATETIMEOFFSET (7) NULL,
    [UserUpdated] INT                NULL,
    CONSTRAINT [PK_ReportShare] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_ReportShare_Report] FOREIGN KEY ([ReportId]) REFERENCES [dbo].[Report] ([Id]),
    CONSTRAINT [FK_ReportShare_UserCreated] FOREIGN KEY ([UserCreated]) REFERENCES [dbo].[User] ([Id]),
    CONSTRAINT [FK_reportShare_UserUpdated] FOREIGN KEY ([UserUpdated]) REFERENCES [dbo].[User] ([Id])
);

