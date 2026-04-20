using System;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using PharmacyServer.Models;

namespace PharmacyClient.Services
{
    /// <summary>
    /// Сервис для автоматического управления учетными записями SQL Server для сотрудников
    /// </summary>
    public class SqlServerUserManagementService
    {
        private readonly string _masterConnectionString;
        private readonly string _databaseName;

        public SqlServerUserManagementService(string databaseConnectionString, string databaseName = "PharmacyDB")
        {
            _databaseName = databaseName;
            
            // Извлекаем строку подключения к master для создания логинов
            var builder = new SqlConnectionStringBuilder(databaseConnectionString)
            {
                InitialCatalog = "master"
            };
            _masterConnectionString = builder.ConnectionString;
        }

        /// <summary>
        /// Создает логин и пользователя SQL Server для нового сотрудника
        /// </summary>
        public async Task CreateUserAsync(Employee employee, string password = "12345678")
        {
            if (string.IsNullOrEmpty(employee.LastName))
                throw new ArgumentException("Фамилия сотрудника не может быть пустой");

            var loginName = Transliterate(employee.LastName);
            var roleName = GetRoleForPosition(employee.Position);

            using (var connection = new SqlConnection(_masterConnectionString))
            {
                await connection.OpenAsync();

                // 1. Создаем LOGIN на уровне сервера (если не существует)
                var checkLoginSql = $"SELECT COUNT(*) FROM sys.server_principals WHERE name = @LoginName";
                using (var checkCmd = new SqlCommand(checkLoginSql, connection))
                {
                    checkCmd.Parameters.AddWithValue("@LoginName", loginName);
                    var exists = (int)await checkCmd.ExecuteScalarAsync();

                    if (exists == 0)
                    {
                        var createLoginSql = $@"
                            CREATE LOGIN [{loginName}] 
                            WITH PASSWORD = '{password}', 
                            CHECK_POLICY = OFF, 
                            DEFAULT_DATABASE = [{_databaseName}]";
                        
                        using (var createCmd = new SqlCommand(createLoginSql, connection))
                        {
                            await createCmd.ExecuteNonQueryAsync();
                        }
                    }
                    else
                    {
                        // Обновляем пароль и БД по умолчанию
                        var updateLoginSql = $@"
                            ALTER LOGIN [{loginName}] 
                            WITH PASSWORD = '{password}', 
                            DEFAULT_DATABASE = [{_databaseName}]";
                        
                        using (var updateCmd = new SqlCommand(updateLoginSql, connection))
                        {
                            await updateCmd.ExecuteNonQueryAsync();
                        }
                    }
                }

                // 2. Переключаемся на базу данных и создаем USER
                connection.ChangeDatabase(_databaseName);

                var checkUserSql = $"SELECT COUNT(*) FROM sys.database_principals WHERE name = @LoginName";
                using (var checkCmd = new SqlCommand(checkUserSql, connection))
                {
                    checkCmd.Parameters.AddWithValue("@LoginName", loginName);
                    var exists = (int)await checkCmd.ExecuteScalarAsync();

                    if (exists == 0)
                    {
                        var createUserSql = $"CREATE USER [{loginName}] FOR LOGIN [{loginName}]";
                        using (var createCmd = new SqlCommand(createUserSql, connection))
                        {
                            await createCmd.ExecuteNonQueryAsync();
                        }
                    }
                }

                // 3. Назначаем роль
                await AssignRoleAsync(connection, loginName, roleName);
            }
        }

        /// <summary>
        /// Удаляет логин и пользователя SQL Server при удалении сотрудника
        /// </summary>
        public async Task DeleteUserAsync(Employee employee)
        {
            if (string.IsNullOrEmpty(employee.LastName))
                return;

            var loginName = Transliterate(employee.LastName);

            using (var connection = new SqlConnection(_masterConnectionString))
            {
                await connection.OpenAsync();

                // Проверяем существование логина
                var checkLoginSql = $"SELECT COUNT(*) FROM sys.server_principals WHERE name = @LoginName";
                using (var checkCmd = new SqlCommand(checkLoginSql, connection))
                {
                    checkCmd.Parameters.AddWithValue("@LoginName", loginName);
                    var exists = (int)await checkCmd.ExecuteScalarAsync();

                    if (exists == 0)
                        return; // Логина нет, ничего не делаем
                }

                // Переключаемся на базу данных
                connection.ChangeDatabase(_databaseName);

                // 1. Удаляем из всех ролей
                var getRolesSql = @"
                    SELECT r.name 
                    FROM sys.database_role_members rm
                    JOIN sys.database_principals r ON rm.role_principal_id = r.principal_id
                    JOIN sys.database_principals m ON rm.member_principal_id = m.principal_id
                    WHERE m.name = @LoginName";

                using (var getRolesCmd = new SqlCommand(getRolesSql, connection))
                {
                    getRolesCmd.Parameters.AddWithValue("@LoginName", loginName);
                    using (var reader = await getRolesCmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var roleName = reader.GetString(0);
                            var dropFromRoleSql = $"ALTER ROLE [{roleName}] DROP MEMBER [{loginName}]";
                            using (var dropCmd = new SqlCommand(dropFromRoleSql, connection))
                            {
                                await dropCmd.ExecuteNonQueryAsync();
                            }
                        }
                    }
                }

                // 2. Удаляем пользователя БД
                var dropUserSql = $"IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = @LoginName) DROP USER [{loginName}]";
                using (var dropUserCmd = new SqlCommand(dropUserSql, connection))
                {
                    dropUserCmd.Parameters.AddWithValue("@LoginName", loginName);
                    await dropUserCmd.ExecuteNonQueryAsync();
                }

                // 3. Возвращаемся к master и удаляем логин
                connection.ChangeDatabase("master");
                var dropLoginSql = $"DROP LOGIN [{loginName}]";
                using (var dropLoginCmd = new SqlCommand(dropLoginSql, connection))
                {
                    await dropLoginCmd.ExecuteNonQueryAsync();
                }
            }
        }

        /// <summary>
        /// Обновляет роль пользователя при изменении должности сотрудника
        /// </summary>
        public async Task UpdateUserRoleAsync(Employee employee)
        {
            if (string.IsNullOrEmpty(employee.LastName))
                return;

            var loginName = Transliterate(employee.LastName);
            var newRoleName = GetRoleForPosition(employee.Position);

            using (var connection = new SqlConnection(_masterConnectionString))
            {
                await connection.OpenAsync();
                connection.ChangeDatabase(_databaseName);

                // Получаем текущие роли пользователя
                var getCurrentRolesSql = @"
                    SELECT r.name 
                    FROM sys.database_role_members rm
                    JOIN sys.database_principals r ON rm.role_principal_id = r.principal_id
                    JOIN sys.database_principals m ON rm.member_principal_id = m.principal_id
                    WHERE m.name = @LoginName";

                using (var getCmd = new SqlCommand(getCurrentRolesSql, connection))
                {
                    getCmd.Parameters.AddWithValue("@LoginName", loginName);
                    using (var reader = await getCmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var currentRole = reader.GetString(0);
                            if (currentRole != newRoleName)
                            {
                                // Удаляем из старой роли
                                var dropSql = $"ALTER ROLE [{currentRole}] DROP MEMBER [{loginName}]";
                                using (var dropCmd = new SqlCommand(dropSql, connection))
                                {
                                    await dropCmd.ExecuteNonQueryAsync();
                                }
                            }
                        }
                    }
                }

                // Добавляем в новую роль
                await AssignRoleAsync(connection, loginName, newRoleName);
            }
        }

        /// <summary>
        /// Сбрасывает пароль пользователя
        /// </summary>
        public async Task ResetPasswordAsync(Employee employee, string newPassword = "12345678")
        {
            if (string.IsNullOrEmpty(employee.LastName))
                throw new ArgumentException("Фамилия сотрудника не может быть пустой");

            var loginName = Transliterate(employee.LastName);

            using (var connection = new SqlConnection(_masterConnectionString))
            {
                await connection.OpenAsync();

                var resetPasswordSql = $@"
                    ALTER LOGIN [{loginName}] 
                    WITH PASSWORD = '{newPassword}'";

                using (var cmd = new SqlCommand(resetPasswordSql, connection))
                {
                    await cmd.ExecuteNonQueryAsync();
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
