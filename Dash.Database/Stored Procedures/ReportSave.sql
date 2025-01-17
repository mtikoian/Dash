﻿CREATE PROCEDURE ReportSave
    @Id INT OUTPUT,
    @DatasetId INT,
    @Name NVARCHAR(100),
    @RowLimit INT = NULL,
    @ChartTypeId INT = NULL,
    @XAxisColumnId INT = NULL,
    @YAxisColumnId INT = NULL,
    @ChartAggregatorId INT = NULL,
    @AggregatorId INT = NULL,
    @ChartDateIntervalId INT = NULL,
    @RequestUserId INT = NULL
AS
    SET NOCOUNT ON

    BEGIN TRY
        IF ISNULL(@Id, 0) = 0
            BEGIN
                INSERT INTO Report (DatasetId, [Name], RowLimit, ChartTypeId, XAxisColumnId, YAxisColumnId, ChartAggregatorId, AggregatorId, ChartDateIntervalId, UserCreated)
                    VALUES (@DatasetId, @Name, @RowLimit, (CASE @ChartTypeId WHEN 0 THEN NULL ELSE @ChartTypeId END),
                    (CASE @XAxisColumnId WHEN 0 THEN NULL ELSE @XAxisColumnId END), (CASE @YAxisColumnId WHEN 0 THEN NULL ELSE @YAxisColumnId END),
                    @ChartAggregatorId, @AggregatorId, @ChartDateIntervalId, @RequestUserId)
                SET @Id = SCOPE_IDENTITY()
            END
        ELSE
            BEGIN
                UPDATE Report SET DatasetId = @DatasetId, [Name] = @Name, RowLimit = @RowLimit,
                    ChartTypeId = (CASE @ChartTypeId WHEN 0 THEN NULL ELSE @ChartTypeId END), XAxisColumnId = (CASE @XAxisColumnId WHEN 0 THEN NULL ELSE @XAxisColumnId END),
                    YAxisColumnId = (CASE @YAxisColumnId WHEN 0 THEN NULL ELSE @YAxisColumnId END),
                    ChartAggregatorId = @ChartAggregatorId, AggregatorId = @AggregatorId, ChartDateIntervalId = @ChartDateIntervalId,
                    UserUpdated = @RequestUserId, DateUpdated = SYSDATETIMEOFFSET()
                WHERE Id = @Id
            END
        RETURN 0
    END TRY
    BEGIN CATCH
        IF XACT_STATE() = -1 ROLLBACK
        DECLARE @ErrorMessage NVARCHAR(4000), @ErrorSeverity INT, @ErrorState INT
        SET @ErrorMessage = CAST (ERROR_NUMBER() AS VARCHAR) + ': ' + ERROR_MESSAGE()
        SET @ErrorSeverity = ERROR_SEVERITY()
        SET @ErrorState = ERROR_STATE()
        RAISERROR (@ErrorMessage, @ErrorSeverity, @ErrorState)
    END CATCH
