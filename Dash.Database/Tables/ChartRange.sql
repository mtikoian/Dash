﻿CREATE TABLE [dbo].[ChartRange] (
    [Id]             INT                IDENTITY (1, 1) NOT NULL,
    [ChartId]        INT                NOT NULL,
    [ReportId]       INT                NOT NULL,
    [XAxisColumnId]  INT                NULL,
    [YAxisColumnId]  INT                NULL,
    [AggregatorId]   INT                NULL,
    [DateIntervalId] INT                NULL,
    [FillDateGaps]   BIT                NULL,
    [Color]          NVARCHAR (20)      NULL,
    [DisplayOrder]   INT                NULL,
    [DateCreated]    DATETIMEOFFSET (7) DEFAULT (sysdatetimeoffset()) NOT NULL,
    [UserCreated]    INT                NULL,
    [DateUpdated]    DATETIMEOFFSET (7) NULL,
    [UserUpdated]    INT                NULL,
    CONSTRAINT [PK_ChartRange] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_ChartRange_DatasetColumn_X] FOREIGN KEY ([XAxisColumnId]) REFERENCES [dbo].[DatasetColumn] ([Id]),
    CONSTRAINT [FK_ChartRange_DatasetColumn_Y] FOREIGN KEY ([YAxisColumnId]) REFERENCES [dbo].[DatasetColumn] ([Id]),
    CONSTRAINT [FK_ChartRange_Chart] FOREIGN KEY ([ChartId]) REFERENCES [dbo].[Chart] ([Id]),
    CONSTRAINT [FK_ChartRange_Report] FOREIGN KEY ([ReportId]) REFERENCES [dbo].[Report] ([Id]),
    CONSTRAINT [FK_ChartRange_UserCreated] FOREIGN KEY ([UserCreated]) REFERENCES [dbo].[User] ([Id]),
    CONSTRAINT [FK_ChartRange_UserUpdated] FOREIGN KEY ([UserUpdated]) REFERENCES [dbo].[User] ([Id])
);


GO
CREATE NONCLUSTERED INDEX [IX_Chart_ChartId]
    ON [dbo].[ChartRange]([ChartId] ASC);

