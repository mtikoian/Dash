CREATE TABLE [dbo].[Chart] (
    [Id]          INT                IDENTITY (1, 1) NOT NULL,
    [Name]        NVARCHAR (100)     NOT NULL,
    [ChartTypeId] INT                NULL,
    [OwnerId]     INT                NULL,
    [DateCreated] DATETIMEOFFSET (7) DEFAULT (sysdatetimeoffset()) NOT NULL,
    [UserCreated] INT                NULL,
    [DateUpdated] DATETIMEOFFSET (7) NULL,
    [UserUpdated] INT                NULL,
    CONSTRAINT [PK_Chart] PRIMARY KEY CLUSTERED ([Id] ASC)
);


GO
CREATE NONCLUSTERED INDEX [IX_Chart_OwnerId]
    ON [dbo].[Chart]([OwnerId] ASC);

