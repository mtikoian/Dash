CREATE TABLE [dbo].[DatasetRole] (
    [Id]          INT                IDENTITY (1, 1) NOT NULL,
    [DatasetId]   INT                NULL,
    [RoleId]      INT                NULL,
    [DateCreated] DATETIMEOFFSET (7) DEFAULT (sysdatetimeoffset()) NOT NULL,
    [UserCreated] INT                NULL,
    [DateUpdated] DATETIMEOFFSET (7) NULL,
    [UserUpdated] INT                NULL,
    CONSTRAINT [PK_DatasetRole] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_DatasetRole_Dataset] FOREIGN KEY ([DatasetId]) REFERENCES [dbo].[Dataset] ([Id]),
    CONSTRAINT [FK_DatasetRole_UserCreated] FOREIGN KEY ([UserCreated]) REFERENCES [dbo].[User] ([Id]),
    CONSTRAINT [FK_DatasetRole_UserUpdated] FOREIGN KEY ([UserUpdated]) REFERENCES [dbo].[User] ([Id])
);

