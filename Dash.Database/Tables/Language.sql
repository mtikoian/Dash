CREATE TABLE [dbo].[Language] (
    [Id]           INT                IDENTITY (1, 1) NOT NULL,
    [Name]         NVARCHAR (100)     NULL,
    [LanguageCode] NVARCHAR (10)      NULL,
    [CountryCode]  NVARCHAR (10)      NULL,
    [DateCreated]  DATETIMEOFFSET (7) DEFAULT (sysdatetimeoffset()) NOT NULL,
    [UserCreated]  INT                NULL,
    [DateUpdated]  DATETIMEOFFSET (7) NULL,
    [UserUpdated]  INT                NULL,
    CONSTRAINT [PK_Language] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Language_UserCreated] FOREIGN KEY ([UserCreated]) REFERENCES [dbo].[User] ([Id]),
    CONSTRAINT [FK_Language_UserUpdated] FOREIGN KEY ([UserUpdated]) REFERENCES [dbo].[User] ([Id])
);

