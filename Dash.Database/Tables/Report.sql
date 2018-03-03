CREATE TABLE [dbo].[Report] (
    [Id]                  INT                IDENTITY (1, 1) NOT NULL,
    [DatasetId]           INT                NOT NULL,
    [Name]                NVARCHAR (100)     NOT NULL,
    [RowLimit]            INT                NULL,
    [XAxisColumnId]       INT                NULL,
    [YAxisColumnId]       INT                NULL,
    [ChartAggregatorId]   INT                NULL,
    [ChartTypeId]         INT                NULL,
    [OwnerId]             INT                NULL,
    [Width]               DECIMAL (18, 14)   NULL,
    [AggregatorId]        INT                NULL,
    [ChartDateIntervalId] INT                NULL,
    [DateCreated]         DATETIMEOFFSET (7) DEFAULT (sysdatetimeoffset()) NOT NULL,
    [UserCreated]         INT                NULL,
    [DateUpdated]         DATETIMEOFFSET (7) NULL,
    [UserUpdated]         INT                NULL,
    CONSTRAINT [PK_Report] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Dataset_Report] FOREIGN KEY ([DatasetId]) REFERENCES [dbo].[Dataset] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Report_DatasetColumn_X] FOREIGN KEY ([XAxisColumnId]) REFERENCES [dbo].[DatasetColumn] ([Id]),
    CONSTRAINT [FK_Report_DatasetColumn_Y] FOREIGN KEY ([YAxisColumnId]) REFERENCES [dbo].[DatasetColumn] ([Id])
);


GO
CREATE NONCLUSTERED INDEX [IX_Report_DatasetId]
    ON [dbo].[Report]([DatasetId] ASC);

