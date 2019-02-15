CREATE TABLE [dbo].[ReportFilter] (
    [Id]           INT                IDENTITY (1, 1) NOT NULL,
    [ReportId]     INT                NOT NULL,
    [ColumnId]     INT                NOT NULL,
    [DisplayOrder] INT                NOT NULL,
    [OperatorId]   INT                NOT NULL,
    [Criteria]     NVARCHAR (4000)     NULL,
    [Criteria2]    NVARCHAR (250)     NULL,
    [DateCreated]  DATETIMEOFFSET (7) DEFAULT (sysdatetimeoffset()) NOT NULL,
    [UserCreated]  INT                NULL,
    [DateUpdated]  DATETIMEOFFSET (7) NULL,
    [UserUpdated]  INT                NULL,
    CONSTRAINT [PK_ReportFilter] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_ReportFilter_DatasetColumn] FOREIGN KEY ([ColumnId]) REFERENCES [dbo].[DatasetColumn] ([Id]),
    CONSTRAINT [FK_ReportFilter_Report] FOREIGN KEY ([ReportId]) REFERENCES [dbo].[Report] ([Id])
);


GO
CREATE NONCLUSTERED INDEX [IX_ReportFilter_ColumnId]
    ON [dbo].[ReportFilter]([ColumnId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_ReportFilter_ReportId]
    ON [dbo].[ReportFilter]([ReportId] ASC);

