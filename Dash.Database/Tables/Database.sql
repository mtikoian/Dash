CREATE TABLE [dbo].[Database] (
    [Id]               INT                IDENTITY (1, 1) NOT NULL,
    [Name]             NVARCHAR (100)     NOT NULL,
    [Host]             NVARCHAR (100)     NULL,
    [TypeId]           TINYINT            NULL,
    [User]             NVARCHAR (100)     NULL,
    [Password]         NVARCHAR (500)     NULL,
    [DatabaseName]     NVARCHAR (100)     NULL,
    [Port]             NVARCHAR (50)      NULL,
    [AllowPaging]      BIT                NULL,
    [ConnectionString] NVARCHAR (500)     NULL,
    [DateCreated]      DATETIMEOFFSET (7) DEFAULT (sysdatetimeoffset()) NOT NULL,
    [UserCreated]      INT                NULL,
    [DateUpdated]      DATETIMEOFFSET (7) NULL,
    [UserUpdated]      INT                NULL,
    CONSTRAINT [PK_Database] PRIMARY KEY CLUSTERED ([Id] ASC)
);

