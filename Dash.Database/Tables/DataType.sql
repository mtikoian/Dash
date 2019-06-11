CREATE TABLE [dbo].[DataType] (
    [Id]          INT                IDENTITY (1, 1) NOT NULL,
    [Name]        NVARCHAR (100)     NULL,
    [IsCurrency]  BIT                NULL,
    [IsDateTime]  BIT                NULL,
    [IsTime]      BIT                NULL,
    [IsDecimal]   BIT                NULL,
    [IsInteger]   BIT                NULL,
    [IsText]      BIT                NULL,
    [IsBool]      BIT                NULL,
    [IsBinary]    BIT                NULL,
    [DateCreated] DATETIMEOFFSET (7) DEFAULT (sysdatetimeoffset()) NOT NULL,
    [UserCreated] INT                NULL,
    [DateUpdated] DATETIMEOFFSET (7) NULL,
    [UserUpdated] INT                NULL,
    CONSTRAINT [PK_DataType] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_DataType_UserCreated] FOREIGN KEY ([UserCreated]) REFERENCES [dbo].[User] ([Id]),
    CONSTRAINT [FK_DataType_UserUpdated] FOREIGN KEY ([UserUpdated]) REFERENCES [dbo].[User] ([Id])
);

