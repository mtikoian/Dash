CREATE TABLE [dbo].[UserRole] (
    [Id]          INT                IDENTITY (1, 1) NOT NULL,
    [UserId]      INT                NULL,
    [RoleId]      INT                NULL,
    [DateCreated] DATETIMEOFFSET (7) DEFAULT (sysdatetimeoffset()) NOT NULL,
    [UserCreated] INT                NULL,
    [DateUpdated] DATETIMEOFFSET (7) NULL,
    [UserUpdated] INT                NULL,
    CONSTRAINT [PK_UserRole] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_UserRole_Role] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Role] ([Id]),
    CONSTRAINT [FK_UserRole_User] FOREIGN KEY ([UserId]) REFERENCES [dbo].[User] ([Id]),
    CONSTRAINT [FK_UserRole_UserCreated] FOREIGN KEY ([UserCreated]) REFERENCES [dbo].[User] ([Id]),
    CONSTRAINT [FK_UserRole_UserUpdated] FOREIGN KEY ([UserUpdated]) REFERENCES [dbo].[User] ([Id])
);

