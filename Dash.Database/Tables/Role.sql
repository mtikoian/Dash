CREATE TABLE [dbo].[Role] (
    [Id]          INT                IDENTITY (1, 1) NOT NULL,
    [Name]        NVARCHAR (100)     NOT NULL,
    [DateCreated] DATETIMEOFFSET (7) DEFAULT (sysdatetimeoffset()) NOT NULL,
    [UserCreated] INT                NULL,
    [DateUpdated] DATETIMEOFFSET (7) NULL,
    [UserUpdated] INT                NULL,
    CONSTRAINT [PK_Role] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Role_UserCreated] FOREIGN KEY ([UserCreated]) REFERENCES [dbo].[User] ([Id]),
    CONSTRAINT [FK_Role_UserUpdated] FOREIGN KEY ([UserUpdated]) REFERENCES [dbo].[User] ([Id])
);

