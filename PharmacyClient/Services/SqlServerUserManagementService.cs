using System;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using PharmacyClient.Models;

namespace PharmacyClient.Services
{
    /// <summary>
    /// Сервис для автоматического управления учетными записями SQL Server для сотрудников
    /// Использует хранимую процедуру sp_CreateDatabaseUser для обхода ограничений прав
    /// </summary>
    public class SqlServerUserManagementService
    {
        private readonly string _connectionString;
        private readonly string _databaseName;

        public SqlServerUserManagementService(string databaseConnectionString, string databaseName = "PharmacyDB")
        {
            _databaseName = databaseName;
            _connectionString = databaseConnectionString;
        }

        /// <summary>
        /// Создает логин и пользователя SQL Server для нового сотрудника через хранимую процедуру
        /// </summary>
        public async Task CreateUserAsync(PharmacyClient.Models.Employee employee, string password = "12345678")
        {
            if (string.IsNullOrEmpty(employee.LastName))
                throw new ArgumentException("Фамилия сотрудника не может быть пустой");

            var loginName = Transliterate(employee.LastName);
            var roleName = GetRoleForPosition(employee.Position);

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Используем хранимую процедуру sp_CreateDatabaseUser для создания пользователя
                // Процедура выполняется с правами EXECUTE AS OWNER, поэтому не требует прав ALTER ANY LOGIN
                var createSql = @"
                    IF OBJECT_ID('master.dbo.sp_CreateDatabaseUser', 'P') IS NOT NULL
                    BEGIN
                        EXEC master.dbo.sp_CreateDatabaseUser 
                            @LoginName = @LoginName,
                            @Password = @Password,
                            @RoleName = @RoleName,
                            @DatabaseName = @DatabaseName;
                    END
                    ELSE
                    BEGIN
                        RAISERROR('Хранимая процедура sp_CreateDatabaseUser не найдена в базе master. Обратитесь к администратору БД.', 16, 1);
                    END";

                using (var cmd = new SqlCommand(createSql, connection))
                {
                    cmd.Parameters.AddWithValue("@LoginName", loginName);
                    cmd.Parameters.AddWithValue("@Password", password);
                    cmd.Parameters.AddWithValue("@RoleName", roleName);
                    cmd.Parameters.AddWithValue("@DatabaseName", _databaseName);

                    try
                    {
                        await cmd.ExecuteNonQueryAsync();
                    }
                    catch (SqlException ex)
                    {
                        throw new Exception($"Ошибка создания пользователя через хранимую процедуру: {ex.Message}", ex);
                    }
                }
            }
        }

        /// <summary>
        /// Удаляет логин и пользователя SQL Server при удалении сотрудника
        /// </summary>
        public async Task DeleteUserAsync(PharmacyClient.Models.Employee employee)
        {
            if (string.IsNullOrEmpty(employee.LastName))
                return;

            var loginName = Transliterate(employee.LastName);

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Переключаемся на базу данных для удаления из ролей и пользователя
                var deleteSql = $@"
                    USE [{_databaseName}];
                    
                    -- 1. Удаляем из всех ролей
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
                        EXEC('ALTER ROLE [' + @oldRole + '] DROP MEMBER [{loginName}]');
                        FETCH NEXT FROM role_cursor INTO @oldRole;
                    END
                    CLOSE role_cursor;
                    DEALLOCATE role_cursor;

                    -- 2. Удаляем пользователя БД
                    IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = @LoginName)
                        DROP USER [{loginName}];
                    
                    -- 3. Возвращаемся к master и удаляем логин
                    USE master;
                    IF EXISTS (SELECT 1 FROM sys.server_principals WHERE name = @LoginName)
                        DROP LOGIN [{loginName}];
                ";

                using (var cmd = new SqlCommand(deleteSql, connection))
                {
                    cmd.Parameters.AddWithValue("@LoginName", loginName);
                    try
                    {
                        await cmd.ExecuteNonQueryAsync();
                    }
                    catch (SqlException ex)
                    {
                        throw new Exception($"Ошибка удаления пользователя: {ex.Message}", ex);
                    }
                }
            }
        }

        /// <summary>
        /// Обновляет роль пользователя при изменении должности сотрудника через хранимую процедуру
        /// </summary>
        public async Task UpdateUserRoleAsync(PharmacyClient.Models.Employee employee)
        {
            if (string.IsNullOrEmpty(employee.LastName))
                return;

            var loginName = Transliterate(employee.LastName);
            var newRoleName = GetRoleForPosition(employee.Position);

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Используем хранимую процедуру с паролем 'unchanged' для обновления только роли
                var updateSql = @"
                    IF OBJECT_ID('master.dbo.sp_CreateDatabaseUser', 'P') IS NOT NULL
                    BEGIN
                        EXEC master.dbo.sp_CreateDatabaseUser 
                            @LoginName = @LoginName,
                            @Password = 'unchanged',
                            @RoleName = @RoleName,
                            @DatabaseName = @DatabaseName;
                    END
                    ELSE
                    BEGIN
                        RAISERROR('Хранимая процедура sp_CreateDatabaseUser не найдена в базе master.', 16, 1);
                    END";

                using (var cmd = new SqlCommand(updateSql, connection))
                {
                    cmd.Parameters.AddWithValue("@LoginName", loginName);
                    cmd.Parameters.AddWithValue("@RoleName", newRoleName);
                    cmd.Parameters.AddWithValue("@DatabaseName", _databaseName);

                    try
                    {
                        await cmd.ExecuteNonQueryAsync();
                    }
                    catch (SqlException ex)
                    {
                        throw new Exception($"Ошибка обновления роли пользователя: {ex.Message}", ex);
                    }
                }
            }
        }

        /// <summary>
        /// Сбрасывает пароль пользователя через хранимую процедуру
        /// </summary>
        public async Task ResetPasswordAsync(PharmacyClient.Models.Employee employee, string newPassword = "12345678")
        {
            if (string.IsNullOrEmpty(employee.LastName))
                throw new ArgumentException("Фамилия сотрудника не может быть пустой");

            var loginName = Transliterate(employee.LastName);

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Используем хранимую процедуру для сброса пароля
                var resetSql = @"
                    IF OBJECT_ID('master.dbo.sp_CreateDatabaseUser', 'P') IS NOT NULL
                    BEGIN
                        EXEC master.dbo.sp_CreateDatabaseUser 
                            @LoginName = @LoginName,
                            @Password = @Password,
                            @RoleName = 'unchanged',
                            @DatabaseName = @DatabaseName;
                    END
                    ELSE
                    BEGIN
                        RAISERROR('Хранимая процедура sp_CreateDatabaseUser не найдена в базе master.', 16, 1);
                    END";

                using (var cmd = new SqlCommand(resetSql, connection))
                {
                    cmd.Parameters.AddWithValue("@LoginName", loginName);
                    cmd.Parameters.AddWithValue("@Password", newPassword);
                    cmd.Parameters.AddWithValue("@DatabaseName", _databaseName);

                    try
                    {
                        await cmd.ExecuteNonQueryAsync();
                    }
                    catch (SqlException ex)
                    {
                        throw new Exception($"Ошибка сброса пароля: {ex.Message}", ex);
                    }
                }
            }
        }

        /// <summary>
        /// Назначает роль пользователю
        /// </summary>
        private async Task AssignRoleAsync(SqlConnection connection, string loginName, string roleName)
        {
            // Проверяем, состоит ли уже в этой роли
            var checkRoleSql = @"
                SELECT COUNT(*) 
                FROM sys.database_role_members rm
                JOIN sys.database_principals r ON rm.role_principal_id = r.principal_id
                JOIN sys.database_principals m ON rm.member_principal_id = m.principal_id
                WHERE m.name = @LoginName AND r.name = @RoleName";

            using (var checkCmd = new SqlCommand(checkRoleSql, connection))
            {
                checkCmd.Parameters.AddWithValue("@LoginName", loginName);
                checkCmd.Parameters.AddWithValue("@RoleName", roleName);
                var exists = (int)await checkCmd.ExecuteScalarAsync();

                if (exists == 0)
                {
                    var assignRoleSql = $"ALTER ROLE [{roleName}] ADD MEMBER [{loginName}]";
                    using (var assignCmd = new SqlCommand(assignRoleSql, connection))
                    {
                        await assignCmd.ExecuteNonQueryAsync();
                    }
                }
            }
        }

        /// <summary>
        /// Транслитерирует русскую фамилию в латиницу
        /// </summary>
        private string Transliterate(string russianText)
        {
            if (string.IsNullOrEmpty(russianText))
                return "unknown";

            var translitMap = new (string rus, string lat)[]
            {
                ("А", "a"), ("Б", "b"), ("В", "v"), ("Г", "g"), ("Д", "d"),
                ("Е", "e"), ("Ё", "yo"), ("Ж", "zh"), ("З", "z"), ("И", "i"),
                ("Й", "y"), ("К", "k"), ("Л", "l"), ("М", "m"), ("Н", "n"),
                ("О", "o"), ("П", "p"), ("Р", "r"), ("С", "s"), ("Т", "t"),
                ("У", "u"), ("Ф", "f"), ("Х", "kh"), ("Ц", "ts"), ("Ч", "ch"),
                ("Ш", "sh"), ("Щ", "sch"), ("Ъ", ""), ("Ы", "y"), ("Ь", ""),
                ("Э", "e"), ("Ю", "yu"), ("Я", "ya"),
                ("а", "a"), ("б", "b"), ("в", "v"), ("г", "g"), ("д", "d"),
                ("е", "e"), ("ё", "yo"), ("ж", "zh"), ("з", "z"), ("и", "i"),
                ("й", "y"), ("к", "k"), ("л", "l"), ("м", "m"), ("н", "n"),
                ("о", "o"), ("п", "p"), ("р", "r"), ("с", "s"), ("т", "t"),
                ("у", "u"), ("ф", "f"), ("х", "kh"), ("ц", "ts"), ("ч", "ch"),
                ("ш", "sh"), ("щ", "sch"), ("ъ", ""), ("ы", "y"), ("ь", ""),
                ("э", "e"), ("ю", "yu"), ("я", "ya")
            };

            var result = russianText.ToLower();
            foreach (var (rus, lat) in translitMap)
            {
                result = result.Replace(rus, lat);
            }

            // Удаляем пробелы и спецсимволы
            result = new string(result.Where(c => char.IsLetterOrDigit(c)).ToArray());

            return string.IsNullOrEmpty(result) ? "unknown" : result;
        }

        /// <summary>
        /// Определяет роль SQL Server на основе должности сотрудника
        /// </summary>
        private string GetRoleForPosition(string position)
        {
            if (string.IsNullOrEmpty(position))
                return "Pharmacy_Staff";

            position = position.ToLower();

            if (position.Contains("директор") || position.Contains("администратор"))
                return "Pharmacy_Admin";
            
            if (position.Contains("заведующий") || position.Contains("менеджер"))
                return "Pharmacy_Manager";
            
            if (position.Contains("главный фармацевт") || position.Contains("технолог") || position.Contains("провизор"))
                return "Pharmacy_Doctor";
            
            if (position.Contains("регистратор"))
                return "Pharmacy_Registrar";

            return "Pharmacy_Staff";
        }
    }
}
