-- ============================================================
-- Хранимая процедура для создания пользователя БД
-- Выполняется с правами EXECUTE AS OWNER, поэтому не требует
-- от вызывающего пользователя прав ALTER ANY LOGIN
-- ============================================================

USE master;
GO

-- Удаляем процедуру если она существует
IF OBJECT_ID('dbo.sp_CreateDatabaseUser', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_CreateDatabaseUser;
GO

CREATE PROCEDURE dbo.sp_CreateDatabaseUser
    @LoginName NVARCHAR(200),
    @Password NVARCHAR(50) = '12345678',
    @RoleName SYSNAME = 'Pharmacy_Staff',
    @DatabaseName SYSNAME = 'PharmacyDB'
WITH EXECUTE AS OWNER
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- 1. Создаем LOGIN на уровне сервера (если не существует)
        DECLARE @IsUpdate BIT = 0;
        DECLARE @sqlCommand NVARCHAR(MAX);
        DECLARE @quotedLoginName SYSNAME;
        
        -- Экранируем имя логина для безопасности
        SET @quotedLoginName = QUOTENAME(@LoginName, '[');

        IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = @LoginName)
        BEGIN
            -- Создаем новый логин
            SET @sqlCommand = N'CREATE LOGIN ' + @quotedLoginName + 
                ' WITH PASSWORD = ''' + REPLACE(@Password, '''', '''''') + 
                ''', CHECK_POLICY = OFF, DEFAULT_DATABASE = [' + @DatabaseName + ']';
            
            EXEC sp_executesql @sqlCommand;
            PRINT 'Логин "' + @LoginName + '" создан.';
        END
        ELSE
        BEGIN
            SET @IsUpdate = 1;
            -- Обновляем пароль только если он не равен 'unchanged'
            IF @Password <> 'unchanged'
            BEGIN
                SET @sqlCommand = N'ALTER LOGIN ' + @quotedLoginName + 
                    ' WITH PASSWORD = ''' + REPLACE(@Password, '''', '''''') + 
                    ''', DEFAULT_DATABASE = [' + @DatabaseName + ']';
                EXEC sp_executesql @sqlCommand;
                PRINT 'Пароль для логина "' + @LoginName + '" обновлен.';
            END
            ELSE
            BEGIN
                PRINT 'Логин "' + @LoginName + '" уже существует, пароль не меняется.';
            END
        END

        -- 2. Переключаемся на базу данных и создаем USER
        SET @sqlCommand = N'
            USE [' + @DatabaseName + '];
            
            -- Создаем USER в базе данных (если не существует)
            IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = @LoginName)
            BEGIN
                CREATE USER ' + @quotedLoginName + ' FOR LOGIN ' + @quotedLoginName + ';
                PRINT ''Пользователь БД "' + @LoginName + '" создан.'';
            END
            ELSE
            BEGIN
                PRINT ''Пользователь БД "' + @LoginName + '" уже существует.'';
            END
            
            -- Обновляем роль только если @RoleName не равен ''unchanged''
            IF @RoleName <> ''unchanged''
            BEGIN
                -- Удаляем из всех ролей перед назначением новой
                DECLARE @oldRole SYSNAME;
                DECLARE role_cursor CURSOR LOCAL FAST_FORWARD FOR
                SELECT r.name FROM sys.database_role_members rm
                JOIN sys.database_principals r ON rm.role_principal_id = r.principal_id
                JOIN sys.database_principals m ON rm.member_principal_id = m.principal_id
                WHERE m.name = @LoginName;

                OPEN role_cursor;
                FETCH NEXT FROM role_cursor INTO @oldRole;
                WHILE @@FETCH_STATUS = 0
                BEGIN
                    EXEC(''ALTER ROLE [' + @oldRole + '] DROP MEMBER [' + @LoginName + ']'');
                    PRINT ''  Удален из роли: '' + @oldRole;
                    FETCH NEXT FROM role_cursor INTO @oldRole;
                END
                CLOSE role_cursor;
                DEALLOCATE role_cursor;

                -- Добавляем в нужную роль
                EXEC(''ALTER ROLE [' + @RoleName + '] ADD MEMBER [' + @LoginName + ']'');
                PRINT ''Назначена роль: '' + @RoleName;
            END
        ';
        
        EXEC sp_executesql @sqlCommand, N'@LoginName SYSNAME, @RoleName SYSNAME', @LoginName = @LoginName, @RoleName = @RoleName;

        PRINT '';
        PRINT '========================================';
        IF @IsUpdate = 0
            PRINT 'Учетная запись успешно создана!';
        ELSE
            PRINT 'Учетная запись успешно обновлена!';
        PRINT '========================================';
        PRINT 'Логин: ' + @LoginName;
        IF @RoleName <> 'unchanged'
            PRINT 'Роль: ' + @RoleName;
        PRINT 'База данных: ' + @DatabaseName;
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

-- Предоставляем право выполнения процедуры всем пользователям
GRANT EXECUTE ON dbo.sp_CreateDatabaseUser TO PUBLIC;
GO

PRINT 'Хранимая процедура sp_CreateDatabaseUser успешно создана в базе master.';
PRINT 'Теперь пользователи могут создавать новых пользователей через эту процедуру.';
PRINT 'Выполняйте её как: EXEC master.dbo.sp_CreateDatabaseUser @LoginName = ''username'', @Password = ''password'', @RoleName = ''Pharmacy_Staff'';';
GO
