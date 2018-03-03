CREATE PROCEDURE ReportFilterSave
	@Id INT OUTPUT,
	@ReportId INT,
	@ColumnId INT,
	@DisplayOrder INT,
	@OperatorId INT,
	@Criteria NVARCHAR(250),
	@Criteria2 NVARCHAR(250) = NULL,
	@RequestUserId INT = NULL
AS
	SET NOCOUNT ON

	BEGIN TRY
		BEGIN TRAN
			UPDATE Report Set DateUpdated = SYSDATETIMEOFFSET(), UserUpdated = @RequestUserId WHERE Id = @ReportId

			IF ISNULL(@Id, 0) = 0
				BEGIN
					INSERT INTO ReportFilter (ReportId, ColumnId, DisplayOrder, OperatorId, Criteria, Criteria2, UserCreated)
						VALUES (@ReportId, @ColumnId, @DisplayOrder, @OperatorId, @Criteria, @Criteria2, @RequestUserId)
					SET @Id = SCOPE_IDENTITY()
				END
			ELSE
				BEGIN
					UPDATE ReportFilter SET	ReportId = @ReportId, ColumnId = @ColumnId, DisplayOrder = @DisplayOrder, OperatorId = @OperatorId, 
						Criteria = @Criteria, Criteria2 = @Criteria2, UserUpdated = @RequestUserId, DateUpdated = SYSDATETIMEOFFSET() 
					WHERE Id = @Id
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