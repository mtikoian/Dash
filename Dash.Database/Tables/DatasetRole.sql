CREATE TABLE [dbo].[DatasetRole] (
    [Id]          INT                IDENTITY (1, 1) NOT NULL,
    [DatasetId]   INT                NULL,
    [RoleId]      INT                NULL,
    [DateCreated] DATETIMEOFFSET (7) DEFAULT (sysdatetimeoffset()) NOT NULL,
    [UserCreated] INT                NULL,
    [DateUpdated] DATETIMEOFFSET (7) NULL,
    [UserUpdated] INT                NULL,
    CONSTRAINT [PK_DatasetRole] PRIMARY KEY CLUSTERED ([Id] ASC)
);

