CREATE TABLE [dbo].[ReportGroup] (
    [Id]           INT                IDENTITY (1, 1) NOT NULL,
    [ReportId]     INT                NOT NULL,
    [ColumnId]     INT                NOT NULL,
    [DisplayOrder] INT                NOT NULL,
    [DateCreated]  DATETIMEOFFSET (7) DEFAULT (sysdatetimeoffset()) NOT NULL,
    [UserCreated]  INT                NULL,
    [DateUpdated]  DATETIMEOFFSET (7) NULL,
    [UserUpdated]  INT                NULL,
    CONSTRAINT [PK_ReportGroup] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_ReportGroup_DatasetColumn] FOREIGN KEY ([ColumnId]) REFERENCES [dbo].[DatasetColumn] ([Id]),
    CONSTRAINT [FK_ReportGroup_Report] FOREIGN KEY ([ReportId]) REFERENCES [dbo].[Report] ([Id]),
    CONSTRAINT [FK_ReportGroup_UserCreated] FOREIGN KEY ([UserCreated]) REFERENCES [dbo].[User] ([Id]),
    CONSTRAINT [FK_ReportGroup_UserUpdated] FOREIGN KEY ([UserUpdated]) REFERENCES [dbo].[User] ([Id])
);

GO
CREATE NONCLUSTERED INDEX [IX_ReportGroup_ColumnId]
    ON [dbo].[ReportGroup]([ColumnId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_ReportGroup_ReportId]
    ON [dbo].[ReportGroup]([ReportId] ASC);

