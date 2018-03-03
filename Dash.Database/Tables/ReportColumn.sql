CREATE TABLE [dbo].[ReportColumn] (
    [Id]            INT                IDENTITY (1, 1) NOT NULL,
    [ReportId]      INT                NOT NULL,
    [ColumnId]      INT                NOT NULL,
    [DisplayOrder]  INT                NULL,
    [Width]         DECIMAL (18, 14)   NULL,
    [SortOrder]     INT                NULL,
    [SortDirection] NVARCHAR (10)      NULL,
    [DateCreated]   DATETIMEOFFSET (7) DEFAULT (sysdatetimeoffset()) NOT NULL,
    [UserCreated]   INT                NULL,
    [DateUpdated]   DATETIMEOFFSET (7) NULL,
    [UserUpdated]   INT                NULL,
    CONSTRAINT [PK_ReportColumn] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Report_ReportColumn] FOREIGN KEY ([ReportId]) REFERENCES [dbo].[Report] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ReportColumn_DatasetColumn] FOREIGN KEY ([ColumnId]) REFERENCES [dbo].[DatasetColumn] ([Id])
);


GO
CREATE NONCLUSTERED INDEX [IX_ReportColumn_ColumnId]
    ON [dbo].[ReportColumn]([ColumnId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_ReportColumn_ReportId]
    ON [dbo].[ReportColumn]([ReportId] ASC);

