CREATE TABLE [dbo].[ChartShare] (
    [Id]          INT                IDENTITY (1, 1) NOT NULL,
    [ChartId]     INT                NOT NULL,
    [UserId]      INT                NULL,
    [RoleId]      INT                NULL,
    [DateCreated] DATETIMEOFFSET (7) DEFAULT (sysdatetimeoffset()) NOT NULL,
    [UserCreated] INT                NULL,
    [DateUpdated] DATETIMEOFFSET (7) NULL,
    [UserUpdated] INT                NULL,
    CONSTRAINT [PK_ChartShare] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_ChartShare_Chart] FOREIGN KEY ([ChartId]) REFERENCES [dbo].[Chart] ([Id])
);

