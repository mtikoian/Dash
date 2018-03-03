CREATE PROCEDURE ChartRangeSave
	@Id INT OUTPUT,
	@ChartId INT,
	@ReportId INT,
	@XAxisColumnId INT,
	@YAxisColumnId INT,
	@AggregatorId INT = NULL,
	@DateIntervalId INT = NULL,
	@DisplayOrder INT = NULL,
	@Color NVARCHAR(20) = NULL,
	@RequestUserId INT = NULL
AS
	SET NOCOUNT ON
	
	BEGIN TRY
		BEGIN TRAN
			UPDATE Chart Set DateUpdated = SYSDATETIMEOFFSET(), UserUpdated = @RequestUserId WHERE Id = @ChartId

			IF ISNULL(@Id, 0) = 0
				BEGIN
					INSERT INTO ChartRange (ChartId, ReportId, XAxisColumnId, YAxisColumnId, AggregatorId, DateIntervalId, DisplayOrder, Color, UserCreated) 
						VALUES (@ChartId, @ReportId, @XAxisColumnId, @YAxisColumnId, @AggregatorId, @DateIntervalId, @DisplayOrder, @Color, @RequestUserId)
					SET @Id = SCOPE_IDENTITY()
				END
			ELSE
				BEGIN
					UPDATE ChartRange SET ChartId = @ChartId, ReportId = @ReportId, XAxisColumnId = @XAxisColumnId, 
						YAxisColumnId = @YAxisColumnId, AggregatorId = @AggregatorId, DateIntervalId = @DateIntervalId, DisplayOrder = @DisplayOrder, Color = @Color,
						UserUpdated = @RequestUserId, DateUpdated = SYSDATETIMEOFFSET() WHERE Id = @Id
				END
		COMMIT
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