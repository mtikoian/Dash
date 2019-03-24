CREATE TABLE [dbo].[Widget] (
    [Id]          INT                IDENTITY (1, 1) NOT NULL,
    [ReportId]    INT                NULL,
    [ChartId]     INT                NULL,
    [RefreshRate] INT                NULL,
    [X]           INT                NULL,
    [Y]           INT                NULL,
    [Width]       INT                NULL,
    [Height]      INT                NULL,
    [Title]       NVARCHAR (100)     NULL,
    [DateCreated] DATETIMEOFFSET (7) DEFAULT (sysdatetimeoffset()) NOT NULL,
    [UserCreated] INT                NULL,
    [DateUpdated] DATETIMEOFFSET (7) NULL,
    [UserUpdated] INT                NULL,
    CONSTRAINT [PK_Widget] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Widget_UserCreated] FOREIGN KEY ([UserCreated]) REFERENCES [dbo].[User] ([Id]),
    CONSTRAINT [FK_Widget_UserUpdated] FOREIGN KEY ([UserUpdated]) REFERENCES [dbo].[User] ([Id])
);


GO
CREATE NONCLUSTERED INDEX [IX_Widget_UserCreated]
    ON [dbo].[Widget]([UserCreated] ASC);

