-- ============================================================
-- Хранимая процедура для создания пользователя БД
-- Выполняется с правами EXECUTE AS OWNER, поэтому не требует
-- от вызывающего пользователя прав ALTER ANY LOGIN
-- ============================================================

USE PharmacyDB;
GO

-- Удаляем процедуру если она существует
IF OBJECT_ID('dbo.sp_CreateDatabaseUser', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_CreateDatabaseUser;
GO

CREATE PROCEDURE dbo.sp_CreateDatabaseUser
    @LoginName NVARCHAR(200),
    @Password NVARCHAR(50) = '12345678',
    @RoleName SYSNAME = 'Pharmacy_Staff'
WITH EXECUTE AS OWNER
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- 1. Создаем LOGIN на уровне сервера (если не существует)
        DECLARE @masterSql NVARCHAR(MAX);
        DECLARE @IsUpdate BIT = 0;

        IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = @LoginName)
        BEGIN
            -- Создаем новый логин
            SET @masterSql = N'CREATE LOGIN [' + @LoginName + '] WITH PASSWORD = ''' + @Password + ''', CHECK_POLICY = OFF, DEFAULT_DATABASE = [PharmacyDB]';
            EXEC(@masterSql);
            PRINT 'Логин "' + @LoginName + '" создан.';
        END
        ELSE
        BEGIN
            SET @IsUpdate = 1;
            -- Обновляем пароль только если он не равен 'unchanged'
            IF @Password <> 'unchanged'
            BEGIN
                SET @masterSql = N'ALTER LOGIN [' + @LoginName + '] WITH PASSWORD = ''' + @Password + ''', DEFAULT_DATABASE = [PharmacyDB]';
                EXEC(@masterSql);
                PRINT 'Пароль для логина "' + @LoginName + '" обновлен.';
            END
            ELSE
            BEGIN
                PRINT 'Логин "' + @LoginName + '" уже существует, пароль не меняется.';
            END
        END

        -- 2. Создаем USER в базе данных (если не существует)
        IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = @LoginName)
        BEGIN
            SET @masterSql = N'CREATE USER [' + @LoginName + '] FOR LOGIN [' + @LoginName + ']';
            EXEC(@masterSql);
            PRINT 'Пользователь БД "' + @LoginName + '" создан.';
        END
        ELSE
        BEGIN
            PRINT 'Пользователь БД "' + @LoginName + '" уже существует.';
        END

        -- 3. Назначаем роль (только если это не просто обновление пароля)
        -- Сначала удаляем из всех ролей
        DECLARE @oldRole SYSNAME;
        DECLARE role_cursor CURSOR FOR
        SELECT r.name FROM sys.database_role_members rm
        JOIN sys.database_principals r ON rm.role_principal_id = r.principal_id
        JOIN sys.database_principals m ON rm.member_principal_id = m.principal_id
        WHERE m.name = @LoginName;

        OPEN role_cursor;
        FETCH NEXT FROM role_cursor INTO @oldRole;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            SET @masterSql = N'ALTER ROLE [' + @oldRole + '] DROP MEMBER [' + @LoginName + ']';
            EXEC(@masterSql);
            PRINT '  Удален из роли: ' + @oldRole;
            FETCH NEXT FROM role_cursor INTO @oldRole;
        END
        CLOSE role_cursor;
        DEALLOCATE role_cursor;

        -- Добавляем в нужную роль
        SET @masterSql = N'ALTER ROLE [' + @RoleName + '] ADD MEMBER [' + @LoginName + ']';
        EXEC(@masterSql);
        PRINT 'Назначена роль: ' + @RoleName;

        PRINT '';
        PRINT '========================================';
        IF @IsUpdate = 0
            PRINT 'Учетная запись успешно создана!';
        ELSE
            PRINT 'Учетная запись успешно обновлена!';
        PRINT '========================================';
        PRINT 'Логин: ' + @LoginName;
        PRINT 'Роль: ' + @RoleName;
        PRINT '========================================';
    END TRY
    BEGIN CATCH
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();

        RAISERROR (@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END
GO

-- Предоставляем право выполнения процедуры всем пользователям базы данных
GRANT EXECUTE ON dbo.sp_CreateDatabaseUser TO PUBLIC;
GO

PRINT 'Хранимая процедура sp_CreateDatabaseUser успешно создана.';
PRINT 'Теперь пользователи могут создавать новых пользователей через эту процедуру.';
GO
