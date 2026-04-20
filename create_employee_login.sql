-- ============================================================
-- Скрипт для создания/обновления учетной записи нового сотрудника в SQL Server
-- Выполните этот скрипт после добавления сотрудника через приложение
-- ============================================================

USE PharmacyDB;
GO

-- Параметры: замените на актуальные значения
DECLARE @EmployeeID INT = 11; -- ID нового сотрудника (получите из таблицы Employees)
DECLARE @LastName NVARCHAR(200) = N'НоваяФамилия'; -- Фамилия сотрудника
DECLARE @FirstName NVARCHAR(200) = N'Имя'; -- Имя сотрудника
DECLARE @Position NVARCHAR(200) = N'Должность'; -- Должность
DECLARE @LoginName NVARCHAR(200) = 'newuser'; -- Логин на латинице (придумайте сами)
DECLARE @Password NVARCHAR(50) = '12345678'; -- Пароль по умолчанию
DECLARE @RoleName SYSNAME = 'Pharmacy_Staff'; -- Роль: Pharmacy_Admin, Pharmacy_Manager, Pharmacy_Doctor, Pharmacy_Staff, Pharmacy_Registrar

-- ============================================================
-- 1. Проверяем, существует ли сотрудник с таким ID
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM dbo.Employees WHERE EmployeeID = @EmployeeID)
BEGIN
    PRINT 'ОШИБКА: Сотрудник с ID ' + CAST(@EmployeeID AS NVARCHAR) + ' не найден в таблице Employees.';
    PRINT 'Сначала добавьте сотрудника через приложение, затем выполните этот скрипт с правильным ID.';
    RETURN;
END

PRINT 'Сотрудник найден: ' + @LastName + ' ' + @FirstName;

-- ============================================================
-- 2. Создаем LOGIN на уровне сервера
-- ============================================================
IF EXISTS (SELECT 1 FROM sys.server_principals WHERE name = @LoginName)
BEGIN
    PRINT 'Логин "' + @LoginName + '" уже существует. Обновляем пароль.';
    EXEC('ALTER LOGIN [' + @LoginName + '] WITH PASSWORD = ''' + @Password + ''', DEFAULT_DATABASE = [PharmacyDB];');
END
ELSE
BEGIN
    PRINT 'Создаем логин "' + @LoginName + '"...';
    EXEC('CREATE LOGIN [' + @LoginName + '] WITH PASSWORD = ''' + @Password + ''', DEFAULT_DATABASE = [PharmacyDB], CHECK_POLICY = OFF;');
END

-- ============================================================
-- 3. Создаем USER в базе данных PharmacyDB
-- ============================================================
IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = @LoginName)
BEGIN
    PRINT 'Пользователь БД "' + @LoginName + '" уже существует.';
END
ELSE
BEGIN
    PRINT 'Создаем пользователя БД "' + @LoginName + '"...';
    EXEC('CREATE USER [' + @LoginName + '] FOR LOGIN [' + @LoginName + '];');
END

-- ============================================================
-- 4. Назначаем роль
-- ============================================================
-- Сначала удаляем из всех ролей, если состоит
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
    EXEC('ALTER ROLE [' + @oldRole + '] DROP MEMBER [' + @LoginName + '];');
    PRINT '  Удален из роли: ' + @oldRole;
    FETCH NEXT FROM role_cursor INTO @oldRole;
END
CLOSE role_cursor;
DEALLOCATE role_cursor;

-- Добавляем в нужную роль
EXEC('ALTER ROLE [' + @RoleName + '] ADD MEMBER [' + @LoginName + '];');
PRINT 'Назначена роль: ' + @RoleName;

-- ============================================================
-- 5. Вывод информации
-- ============================================================
PRINT '';
PRINT '========================================';
PRINT 'Учетная запись успешно создана!';
PRINT '========================================';
PRINT 'Логин: ' + @LoginName;
PRINT 'Пароль: ' + @Password;
PRINT 'Роль: ' + @RoleName;
PRINT 'Сотрудник: ' + @LastName + ' ' + @FirstName;
PRINT '========================================';
PRINT '';
PRINT 'Теперь сотрудник может войти в систему под этим логином.';
GO

-- ============================================================
-- ПРИМЕР использования (раскомментируйте и измените значения):
-- ============================================================
/*
DECLARE @EmployeeID INT = 11;
DECLARE @LastName NVARCHAR(200) = N'Петров';
DECLARE @FirstName NVARCHAR(200) = N'Петр';
DECLARE @Position NVARCHAR(200) = N'Фармацевт';
DECLARE @LoginName NVARCHAR(200) = 'petrov';
DECLARE @Password NVARCHAR(50) = '12345678';
DECLARE @RoleName SYSNAME = 'Pharmacy_Staff';
*/
