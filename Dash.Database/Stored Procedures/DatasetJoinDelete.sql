CREATE PROCEDURE DatasetJoinDelete
	@Id INT,
	@RequestUserId INT = NULL
AS
	SET NOCOUNT ON

	BEGIN TRY
        DECLARE @DatasetId INT
        DECLARE @JoinOrder INT
        SELECT TOP 1 @DatasetId = DatasetId, @JoinOrder = JoinOrder FROM DatasetJoin WHERE Id = @Id
		DELETE FROM dbo.DatasetJoin WHERE Id = @Id
        UPDATE DatasetJoin SET JoinOrder = JoinOrder - 1 WHERE DatasetId = @DatasetId AND JoinOrder > @JoinOrder
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
