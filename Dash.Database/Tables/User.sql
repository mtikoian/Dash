CREATE TABLE [dbo].[User] (
    [Id]                INT                IDENTITY (1, 1) NOT NULL,
    [UserName]          NVARCHAR (100)     NULL,
    [FirstName]         NVARCHAR (100)     NULL,
    [LastName]          NVARCHAR (100)     NULL,
    [LanguageId]        INT                NULL,
    [Email]             NVARCHAR (100)     NULL,
    [AllowSingleFactor] BIT                DEFAULT ((0)) NOT NULL,
    [Password]          NVARCHAR (500)     NULL,
    [Salt]              NVARCHAR (500)     NULL,
    [ResetHash]         NVARCHAR (500)     NULL,
    [DateReset]         DATETIMEOFFSET (7) NULL,
    [LoginHash]         NVARCHAR(50)       NULL, 
    [DateLoginWindow]   DATETIMEOFFSET     NULL, 
    [LoginAttempts]     INT                NULL, 
    [DateUnlocks]       DATETIMEOFFSET     NULL, 
    [Status]            BIT                DEFAULT ((1)) NULL,
    [DateCreated]       DATETIMEOFFSET (7) DEFAULT (sysdatetimeoffset()) NOT NULL,
    [UserCreated]       INT                NULL,
    [DateUpdated]       DATETIMEOFFSET (7) NULL,
    [UserUpdated]       INT                NULL,
    CONSTRAINT [PK_User] PRIMARY KEY CLUSTERED ([Id] ASC)
);

