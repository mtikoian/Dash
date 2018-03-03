CREATE PROCEDURE WidgetSave
	@Id INT OUTPUT,
	@UserId INT = NULL,
	@ReportId INT = NULL,
	@ChartId INT = NULL,
	@RefreshRate INT = NULL,
	@X INT = NULL,
	@Y INT = NULL,
	@Width INT = NULL,
	@Height INT = NULL,
	@Title NVARCHAR(100) = NULL,
	@RequestUserId INT = NULL
AS
	SET NOCOUNT ON

	BEGIN TRY
		IF ISNULL(@Id, 0) = 0
			BEGIN
				INSERT INTO Widget ([UserId], [ReportId], ChartId, RefreshRate, X, Y, Width, Height, Title, UserCreated)
					VALUES (@UserId, @ReportId, @ChartId, @RefreshRate, @X, @Y, @Width, @Height, @Title, @RequestUserId)
				SET @Id = SCOPE_IDENTITY()
			END
		ELSE
			BEGIN
				UPDATE Widget SET [UserId] = @UserId, [ReportId] = @ReportId, ChartId = @ChartId, RefreshRate = @RefreshRate, X = @X, 
					Y = @Y, Width = @Width, Height = @Height, Title = @Title, UserUpdated = @RequestUserId, DateUpdated = SYSDATETIMEOFFSET() WHERE Id = @Id
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