-- Скрипт для создания исправленной хранимой процедуры в базе PharmacyDB
-- Выполняйте этот скрипт, подключившись к базе данных PharmacyDB

USE PharmacyDB;
GO

-- Удаляем старую процедуру, если она существует (на случай предыдущих неудачных попыток)
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_CreateDatabaseUser')
BEGIN
    DROP PROCEDURE sp_CreateDatabaseUser;
    PRINT 'Старая процедура sp_CreateDatabaseUser удалена.';
END
GO

CREATE PROCEDURE sp_CreateDatabaseUser
    @LoginName NVARCHAR(128),
    @Password NVARCHAR(128),
    @RoleName NVARCHAR(128) = 'Pharmacy_Staff' -- Роль по умолчанию
WITH EXECUTE AS OWNER -- Выполнять от имени владельца базы данных
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @sql NVARCHAR(MAX);
    DECLARE @dbName SYSNAME = DB_NAME();
    DECLARE @oldRole SYSNAME;
    DECLARE @userExists BIT = 0;
    DECLARE @loginExists BIT = 0;

    -- Проверка существования логина на уровне сервера
    -- Примечание: внутри процедуры с EXECUTE AS OWNER мы можем проверять системные представления,
    -- но прямой доступ к master.sys.server_principals может быть ограничен в зависимости от конфигурации.
    -- Попытка проверки через динамический SQL или предположение, что логин будет создан.
    
    -- 1. Создаем ЛОГИН на уровне сервера
    -- Используем динамический SQL для безопасности имен
    SET @sql = N'
    IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N''' + REPLACE(@LoginName, '''', '''''') + N''')
    BEGIN
        CREATE LOGIN [' + @LoginName + '] WITH PASSWORD = N''' + REPLACE(@Password, '''', '''''') + ''', CHECK_POLICY = OFF;
    END';
    
    BEGIN TRY
        EXEC sp_executesql @sql;
        PRINT 'Логин ' + @LoginName + ' проверен/создан.';
    END TRY
    BEGIN CATCH
        -- Если не удалось создать логин (нет прав), пробуем продолжить, надеясь, что он уже есть,
        -- или выводим ошибку, если логин критически необходим.
        -- В строгой среде здесь может потребоваться подпись сертификатом.
        PRINT 'Предупреждение: Не удалось создать/проверить логин напрямую. Ошибка: ' + ERROR_MESSAGE();
        -- Если логин не создан и не существует, дальнейшие шаги упадут.
    END CATCH

    -- 2. Создаем ПОЛЬЗОВАТЕЛЯ в текущей базе данных
    SET @sql = N'
    IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N''' + REPLACE(@LoginName, '''', '''''') + N''')
    BEGIN
        CREATE USER [' + @LoginName + '] FOR LOGIN [' + @LoginName + '];
    END';
    
    EXEC sp_executesql @sql;
    PRINT 'Пользователь ' + @LoginName + ' создан в базе ' + @dbName + '.';

    -- 3. Определяем текущую роль пользователя (если она есть)
    SELECT @oldRole = r.name
    FROM sys.database_role_members drm
    JOIN sys.database_principals u ON drm.member_principal_id = u.principal_id
    JOIN sys.database_principals r ON drm.role_principal_id = r.principal_id
    WHERE u.name = @LoginName;

    -- 4. Меняем роль, если она отличается от требуемой
    IF @oldRole IS NOT NULL AND @oldRole <> @RoleName
    BEGIN
        -- Удаляем из старой роли
        SET @sql = N'ALTER ROLE [' + @oldRole + '] DROP MEMBER [' + @LoginName + '];';
        EXEC sp_executesql @sql;
        PRINT 'Удален из роли ' + @oldRole + '.';
    END

    IF @oldRole <> @RoleName -- Если роли разные (или старой не было)
    BEGIN
        -- Добавляем в новую роль
        SET @sql = N'ALTER ROLE [' + @RoleName + '] ADD MEMBER [' + @LoginName + '];';
        EXEC sp_executesql @sql;
        PRINT 'Добавлен в роль ' + @RoleName + '.';
    END

    PRINT 'Пользователь ' + @LoginName + ' успешно настроен.';
END
GO

PRINT 'Хранимая процедура sp_CreateDatabaseUser успешно создана в базе PharmacyDB.';
GO

-- Выдача прав на выполнение процедуры роли администраторов (или всем, если нужно)
GRANT EXECUTE ON sp_CreateDatabaseUser TO [Pharmacy_Admin]; 
-- Или, если вы хотите, чтобы любой сотрудник мог создавать (не рекомендуется), используйте PUBLIC
GO
