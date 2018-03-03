﻿CREATE TABLE [dbo].[DatasetJoin] (
    [Id]          INT                IDENTITY (1, 1) NOT NULL,
    [DatasetId]   INT                NOT NULL,
    [TableName]   NVARCHAR (100)     NOT NULL,
    [JoinTypeId]  INT                NOT NULL,
    [Keys]        NVARCHAR (500)     NULL,
    [JoinOrder]   INT                NULL,
    [DateCreated] DATETIMEOFFSET (7) DEFAULT (sysdatetimeoffset()) NOT NULL,
    [UserCreated] INT                NULL,
    [DateUpdated] DATETIMEOFFSET (7) NULL,
    [UserUpdated] INT                NULL,
    CONSTRAINT [PK_DatasetJoin] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Dataset_DatasetJoin] FOREIGN KEY ([DatasetId]) REFERENCES [dbo].[Dataset] ([Id]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_DatasetJoin_DatasetId]
    ON [dbo].[DatasetJoin]([DatasetId] ASC);

