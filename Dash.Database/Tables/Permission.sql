CREATE TABLE [dbo].[Permission] (
    [Id]             INT                IDENTITY (1, 1) NOT NULL,
    [ControllerName] NVARCHAR (100)     NULL,
    [ActionName]     NVARCHAR (100)     NULL,
    [DateCreated]    DATETIMEOFFSET (7) DEFAULT (sysdatetimeoffset()) NOT NULL,
    [UserCreated]    INT                NULL,
    [DateUpdated]    DATETIMEOFFSET (7) NULL,
    [UserUpdated]    INT                NULL,
    CONSTRAINT [PK_Permission] PRIMARY KEY CLUSTERED ([Id] ASC)
);

