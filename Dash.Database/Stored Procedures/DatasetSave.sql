CREATE PROCEDURE DatasetSave
	@Id INT OUTPUT,
	@DatabaseId INT,
	@Name NVARCHAR(100),
	@PrimarySource NVARCHAR(100),	
	@TypeId TINYINT,	
	@DateFormat NVARCHAR(50),	
	@CurrencyFormat NVARCHAR(50),	
	@Description NVARCHAR(500) = NULL,	
	@Conditions NVARCHAR(250) = NULL,	
	@RequestUserId INT = NULL
AS
	SET NOCOUNT ON

	BEGIN TRY
		IF ISNULL(@Id, 0) = 0
			BEGIN
				INSERT INTO Dataset (DatabaseId, Name, [Description], PrimarySource, TypeId, Conditions, [DateFormat], CurrencyFormat, UserCreated)
					VALUES (@DatabaseId, @Name, @Description, @PrimarySource, @TypeId, @Conditions, @DateFormat, @CurrencyFormat, @RequestUserId)
				SET @Id = SCOPE_IDENTITY()
			END
		ELSE
			BEGIN
				UPDATE Dataset SET Name = @Name, DatabaseId = @DatabaseId, [Description] = @Description, PrimarySource = @PrimarySource, TypeId = @TypeId,
					Conditions = @Conditions, [DateFormat] = @DateFormat, CurrencyFormat = @CurrencyFormat, 
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
