CREATE TABLE [dbo].[ReportFilterCriteria] (
    [Id]           INT                IDENTITY (1, 1) NOT NULL,
    [ReportFilterId]     INT                NOT NULL,
    [Value]    NVARCHAR (250)     NULL,
    [DateCreated]  DATETIMEOFFSET (7) DEFAULT (sysdatetimeoffset()) NOT NULL,
    [UserCreated]  INT                NULL,
    CONSTRAINT [PK_ReportFilterCriteria] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_ReportFilterCriteria_ReportFilter] FOREIGN KEY ([ReportFilterId]) REFERENCES [dbo].[ReportFilter] ([Id]),
    CONSTRAINT [FK_ReportFilterCriteria_UserCreated] FOREIGN KEY ([UserCreated]) REFERENCES [dbo].[User] ([Id])
);


GO
CREATE NONCLUSTERED INDEX [IX_ReportFilterCriteria_ReportFilterId]
    ON [dbo].[ReportFilterCriteria]([ReportFilterId] ASC);

