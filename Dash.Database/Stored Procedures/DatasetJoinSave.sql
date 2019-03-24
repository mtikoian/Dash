CREATE PROCEDURE DatasetJoinSave
    @Id INT OUTPUT,
    @DatasetId INT,
    @TableName NVARCHAR(100),
    @JoinTypeId INT,
    @Keys NVARCHAR(500),
    @JoinOrder INT,
    @RequestUserId INT = NULL
AS
    SET NOCOUNT ON

    BEGIN TRY
        IF ISNULL (@Id, 0) = 0
            BEGIN
                INSERT INTO DatasetJoin (DatasetId, TableName, JoinTypeId, Keys, JoinOrder, UserCreated)
                    VALUES (@DatasetId, @TableName, @JoinTypeId, @Keys, @JoinOrder, @RequestUserId)
                SET @Id = SCOPE_IDENTITY()
            END
        ELSE
            BEGIN
                UPDATE DatasetJoin SET DatasetId = @DatasetId, TableName = @TableName, JoinTypeId = @JoinTypeId,
                    Keys = @keys, JoinOrder = @JoinOrder, UserUpdated = @RequestUserId, DateUpdated = SYSDATETIMEOFFSET()
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
