CREATE PROCEDURE ReportFilterCriteriaSave
	@Id INT OUTPUT,
	@ReportFilterId INT,
	@Value NVARCHAR(250) = NULL,
	@RequestUserId INT = NULL
AS
	SET NOCOUNT ON

	BEGIN TRY
		BEGIN TRAN
            UPDATE r SET DateUpdated = SYSDATETIMEOFFSET(), UserUpdated = @RequestUserId
                FROM Report r
                INNER JOIN ReportFilter rf ON rf.ReportId = r.Id
                WHERE rf.Id = @ReportFilterId

			IF ISNULL(@Id, 0) = 0
				BEGIN
					INSERT INTO ReportFilterCriteria (ReportFilterId, [Value], UserCreated)
						VALUES (@ReportFilterId, @Value, @RequestUserId)
					SET @Id = SCOPE_IDENTITY()
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
