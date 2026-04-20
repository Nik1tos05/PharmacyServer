-- ============================================================
-- Скрипт для удаления учетной записи сотрудника из SQL Server
-- Выполните этот скрипт после удаления сотрудника из приложения
-- ============================================================

USE PharmacyDB;
GO

-- Параметр: логин пользователя на латинице
DECLARE @LoginName NVARCHAR(200) = 'userToDelete'; -- Замените на актуальный логин

-- ============================================================
-- 1. Проверяем существование пользователя
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = @LoginName)
BEGIN
    PRINT 'Пользователь БД "' + @LoginName + '" не найден.';
END
ELSE
BEGIN
    PRINT 'Найден пользователь БД: ' + @LoginName;
    
    -- Удаляем из всех ролей
    DECLARE @roleName SYSNAME;
    DECLARE role_cursor CURSOR FOR 
    SELECT r.name FROM sys.database_role_members rm
    JOIN sys.database_principals r ON rm.role_principal_id = r.principal_id
    JOIN sys.database_principals m ON rm.member_principal_id = m.principal_id
    WHERE m.name = @LoginName;

    OPEN role_cursor;
    FETCH NEXT FROM role_cursor INTO @roleName;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        EXEC('ALTER ROLE [' + @roleName + '] DROP MEMBER [' + @LoginName + '];');
        PRINT '  Удален из роли: ' + @roleName;
        FETCH NEXT FROM role_cursor INTO @roleName;
    END
    CLOSE role_cursor;
    DEALLOCATE role_cursor;

    -- Удаляем пользователя БД
    EXEC('DROP USER [' + @LoginName + '];');
    PRINT 'Пользователь БД "' + @LoginName + '" удален.';
END

-- ============================================================
-- 2. Удаляем LOGIN с уровня сервера
-- ============================================================
IF EXISTS (SELECT 1 FROM sys.server_principals WHERE name = @LoginName)
BEGIN
    EXEC('DROP LOGIN [' + @LoginName + '];');
    PRINT 'Логин сервера "' + @LoginName + '" удален.';
END
ELSE
BEGIN
    PRINT 'Логин сервера "' + @LoginName + '" не найден.';
END

PRINT '';
PRINT 'Учетная запись "' + @LoginName + '" успешно удалена из SQL Server.';
GO

-- ============================================================
-- ПРИМЕР использования (раскомментируйте и измените значение):
-- ============================================================
/*
DECLARE @LoginName NVARCHAR(200) = 'petrov';
EXEC sp_executesql N'DECLARE @LoginName NVARCHAR(200) = ''petrov''; ...'; -- вставьте полный скрипт выше
*/
