CREATE TABLE [dbo].[Chart] (
    [Id]          INT                IDENTITY (1, 1) NOT NULL,
    [Name]        NVARCHAR (100)     NOT NULL,
    [ChartTypeId] INT                NULL,
    [DateCreated] DATETIMEOFFSET (7) DEFAULT (sysdatetimeoffset()) NOT NULL,
    [UserCreated] INT                NULL,
    [DateUpdated] DATETIMEOFFSET (7) NULL,
    [UserUpdated] INT                NULL,
    CONSTRAINT [PK_Chart] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Chart_UserCreated] FOREIGN KEY ([UserCreated]) REFERENCES [dbo].[User] ([Id]),
    CONSTRAINT [FK_Chart_UserUpdated] FOREIGN KEY ([UserUpdated]) REFERENCES [dbo].[User] ([Id])
);
GO

CREATE NONCLUSTERED INDEX [IX_Chart_UserCreated]
    ON [dbo].[Chart]([UserCreated] ASC);
GO
