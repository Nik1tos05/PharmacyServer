-- ============================================================
-- Хранимая процедура для удаления лекарства
-- Выполняется с правами EXECUTE AS OWNER, поэтому не требует
-- от вызывающего пользователя прав DELETE на таблицу Medicines
-- ============================================================

USE PharmacyDB;
GO

-- Удаляем процедуру если она существует
IF OBJECT_ID('dbo.sp_DeleteMedicine', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_DeleteMedicine;
GO

CREATE PROCEDURE dbo.sp_DeleteMedicine
    @MedicineId INT
WITH EXECUTE AS OWNER
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- 1. Сначала удаляем связанные записи из MedicineComposition
        DELETE FROM dbo.MedicineCompositions
        WHERE MedicineId = @MedicineId;

        -- 2. Затем удаляем само лекарство
        DELETE FROM dbo.Medicines
        WHERE Id = @MedicineId;

        COMMIT TRANSACTION;

        PRINT 'Лекарство с ID ' + CAST(@MedicineId AS NVARCHAR(10)) + ' успешно удалено.';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();

        RAISERROR (@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END
GO

-- Предоставляем право выполнения процедуры всем пользователям
GRANT EXECUTE ON dbo.sp_DeleteMedicine TO PUBLIC;
GO

PRINT 'Хранимая процедура sp_DeleteMedicine успешно создана в базе PharmacyDB.';
PRINT 'Теперь пользователи могут удалять лекарства через эту процедуру.';
PRINT 'Выполняйте её как: EXEC dbo.sp_DeleteMedicine @MedicineId = 1;';
GO
