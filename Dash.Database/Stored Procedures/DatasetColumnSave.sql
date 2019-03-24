CREATE PROCEDURE DatasetColumnSave
    @Id INT OUTPUT,
    @DatasetId INT,
    @Title NVARCHAR(100),
    @ColumnName NVARCHAR(250),
    @DataTypeId INT,
    @Derived NVARCHAR(500) = NULL,
    @FilterTypeId INT = NULL,
    @FilterQuery NVARCHAR(500) = NULL,
    @Link NVARCHAR(250) = NULL,
    @IsParam BIT = 0,
    @RequestUserId INT = NULL
AS
    SET NOCOUNT ON

    BEGIN TRY
        IF ISNULL (@Id, 0) = 0
            BEGIN
                INSERT INTO DatasetColumn (DatasetId, Title, ColumnName, Derived, DataTypeId, FilterTypeId, FilterQuery, Link, IsParam, UserCreated)
                    VALUES (@DatasetId, @Title, @ColumnName, @Derived, @DataTypeId, @FilterTypeId, @FilterQuery, @Link, @IsParam, @RequestUserId)
                SET @Id = SCOPE_IDENTITY()
            END
        ELSE
            BEGIN
                UPDATE DatasetColumn SET DatasetId = @DatasetId, Title = @Title, ColumnName = @ColumnName, Derived = @Derived,
                    DataTypeId = @DataTypeId, FilterTypeId = @FilterTypeId, FilterQuery = @FilterQuery, Link = @Link, IsParam = @IsParam,
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
