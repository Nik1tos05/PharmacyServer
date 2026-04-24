-- Полный скрипт настройки прав доступа и хранимых процедур для PharmacyDB
-- Выполнять от имени администратора БД (dbo)

USE PharmacyDB;
GO

-- ============================================================
-- ЧАСТЬ 1: Создание хранимой процедуры для удаления лекарств
-- ============================================================

-- Удаляем существующую процедуру если есть
IF OBJECT_ID('dbo.sp_DeleteMedicine', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_DeleteMedicine;
GO

-- Создаем хранимую процедуру для безопасного удаления лекарства
-- Процедура выполняется с правами владельца БД (EXECUTE AS OWNER),
-- поэтому не требует от вызывающего пользователя прямых прав DELETE на таблицы
CREATE PROCEDURE dbo.sp_DeleteMedicine
    @MedicineId INT
WITH EXECUTE AS OWNER
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Сначала удаляем связанные записи из состава лекарств
        DELETE FROM dbo.MedicineComposition WHERE MedicineId = @MedicineId;
        
        -- Затем удаляем само лекарство
        DELETE FROM dbo.Medicines WHERE MedicineId = @MedicineId;
        
        COMMIT TRANSACTION;
        
        PRINT 'Лекарство успешно удалено';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
            
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END
GO

-- Предоставляем право на выполнение процедуры всем пользователям
GRANT EXECUTE ON dbo.sp_DeleteMedicine TO PUBLIC;
GO

-- ============================================================
-- ЧАСТЬ 2: Прямые права доступа к таблицам для Pharmacy_Staff
-- ============================================================

-- Таблица Лекарства (Medicines) - полный доступ для персонала
GRANT SELECT, INSERT, UPDATE, DELETE ON dbo.Medicines TO Pharmacy_Staff;

-- Таблица Состав лекарств (MedicineComposition) - необходим для удаления лекарства со сложным составом
GRANT SELECT, INSERT, UPDATE, DELETE ON dbo.MedicineComposition TO Pharmacy_Staff;

-- Таблица Цен (MedicinePrices) - управление ценами доступно персоналу
GRANT SELECT, INSERT, UPDATE, DELETE ON dbo.MedicinePrices TO Pharmacy_Staff;

-- Таблица Типов лекарств (MedicineTypes)
GRANT SELECT, INSERT, UPDATE, DELETE ON dbo.MedicineTypes TO Pharmacy_Staff;

-- Таблица Производителей (Manufacturers)
GRANT SELECT, INSERT, UPDATE, DELETE ON dbo.Manufacturers TO Pharmacy_Staff;

-- Таблица Единиц измерения (Units)
GRANT SELECT, INSERT, UPDATE, DELETE ON dbo.Units TO Pharmacy_Staff;

-- Таблица Категорий лекарств (MedicineCategories)
GRANT SELECT, INSERT, UPDATE, DELETE ON dbo.MedicineCategories TO Pharmacy_Staff;

-- ============================================================
-- ЧАСТЬ 3: Проверка настроенных прав
-- ============================================================

PRINT '===============================================';
PRINT 'Настройка прав доступа завершена успешно!';
PRINT '===============================================';
PRINT 'Создана хранимая процедура: sp_DeleteMedicine';
PRINT 'Процедура выполняется с правами EXECUTE AS OWNER';
PRINT 'Предоставлены права для роли: Pharmacy_Staff';
PRINT '===============================================';
PRINT 'Теперь пользователи могут удалять лекарства:';
PRINT '1. Через хранимую процедуру (рекомендуется)';
PRINT '2. Напрямую, если есть права DELETE на таблицы';
PRINT '===============================================';
GO
