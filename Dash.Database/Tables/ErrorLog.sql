CREATE TABLE [dbo].[ErrorLog]
(
    [Id]           BIGINT            IDENTITY (1, 1) NOT NULL,
    [Namespace] NVARCHAR(100)  COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [Host]        NVARCHAR(100)  COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [Type]        NVARCHAR(100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [Source]      NVARCHAR(100)  COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [Path]      NVARCHAR(100)  COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [Method]      NVARCHAR(100)  COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [Message]     NVARCHAR(500) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [User]        NVARCHAR(50)  COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [Timestamp]     DATETIMEOFFSET NOT NULL,
    [StackTrace]      NTEXT COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
    CONSTRAINT [PK_ErrorLog] PRIMARY KEY CLUSTERED ([Id] ASC)
);