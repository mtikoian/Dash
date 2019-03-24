CREATE TABLE [dbo].[DatasetColumn] (
    [Id]           INT                IDENTITY (1, 1) NOT NULL,
    [DatasetId]    INT                NOT NULL,
    [Title]        NVARCHAR (100)     NOT NULL,
    [ColumnName]   NVARCHAR (250)     NULL,
    [Derived]      NVARCHAR (500)     NULL,
    [FilterTypeId] INT                NULL,
    [FilterQuery]  NVARCHAR (500)     NULL,
    [IsParam]      BIT                DEFAULT ((0)) NOT NULL,
    [DataTypeId]   INT                NULL,
    [Link]         NVARCHAR (250)     NULL,
    [DateCreated]  DATETIMEOFFSET (7) DEFAULT (sysdatetimeoffset()) NOT NULL,
    [UserCreated]  INT                NULL,
    [DateUpdated]  DATETIMEOFFSET (7) NULL,
    [UserUpdated]  INT                NULL,
    CONSTRAINT [PK_DatasetColumn] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_DatasetColumn_Dataset] FOREIGN KEY ([DatasetId]) REFERENCES [dbo].[Dataset] ([Id]),
    CONSTRAINT [FK_DatasetColumn_DataType] FOREIGN KEY ([DataTypeId]) REFERENCES [dbo].[DataType] ([Id]),
    CONSTRAINT [FK_DatasetColumn_UserCreated] FOREIGN KEY ([UserCreated]) REFERENCES [dbo].[User] ([Id]),
    CONSTRAINT [FK_DatasetColumn_UserUpdated] FOREIGN KEY ([UserUpdated]) REFERENCES [dbo].[User] ([Id])
);


GO
CREATE NONCLUSTERED INDEX [IX_DatasetColumn_DatasetId]
    ON [dbo].[DatasetColumn]([DatasetId] ASC);

