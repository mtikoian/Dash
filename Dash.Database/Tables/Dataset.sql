﻿CREATE TABLE [dbo].[Dataset] (
    [Id]             INT                IDENTITY (1, 1) NOT NULL,
    [Name]           NVARCHAR (100)     NOT NULL,
    [TypeId]         TINYINT            NULL,
    [PrimarySource]  NVARCHAR (100)     NULL,
    [Conditions]     NVARCHAR (500)     NULL,
    [DatabaseId]     INT                NULL,
    [DateFormat]     NVARCHAR (50)      NULL,
    [TimeFormat]     NVARCHAR (50)      NULL,
    [CurrencyFormat] NVARCHAR (50)      NULL,
    [DateCreated]    DATETIMEOFFSET (7) DEFAULT (sysdatetimeoffset()) NOT NULL,
    [UserCreated]    INT                NULL,
    [DateUpdated]    DATETIMEOFFSET (7) NULL,
    [UserUpdated]    INT                NULL,
    CONSTRAINT [PK_Dataset] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Dataset_Database] FOREIGN KEY ([DatabaseId]) REFERENCES [dbo].[Database] ([Id]),
    CONSTRAINT [FK_Dataset_UserCreated] FOREIGN KEY ([UserCreated]) REFERENCES [dbo].[User] ([Id]),
    CONSTRAINT [FK_Dataset_UserUpdated] FOREIGN KEY ([UserUpdated]) REFERENCES [dbo].[User] ([Id])
);


GO
CREATE NONCLUSTERED INDEX [IX_Dataset_UserCreated]
    ON [dbo].[Dataset]([UserCreated] ASC);


