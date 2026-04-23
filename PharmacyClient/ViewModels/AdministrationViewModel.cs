using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using PharmacyClient.Data;
using PharmacyClient.Models;

namespace PharmacyClient.ViewModels
{
    public partial class AdministrationViewModel : ObservableObject
    {
        private readonly PharmacyDbContext _context;

        [ObservableProperty]
        private ObservableCollection<UserAccountInfo> _userAccounts = new();

        [ObservableProperty]
        private UserAccountInfo? _selectedUser;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _newLoginName = string.Empty;

        [ObservableProperty]
        private string _newPassword = "12345678";

        [ObservableProperty]
        private string _selectedRole = "Pharmacy_Staff";

        [ObservableProperty]
        private ObservableCollection<string> _availableRoles = new()
        {
            "Pharmacy_Admin",
            "Pharmacy_Manager",
            "Pharmacy_Doctor",
            "Pharmacy_Staff",
            "Pharmacy_Registrar"
        };

        [ObservableProperty]
        private int? _selectedEmployeeId;

        [ObservableProperty]
        private ObservableCollection<EmployeeInfo> _availableEmployees = new();

        public AdministrationViewModel()
        {
            _context = new PharmacyDbContext();
        }

        [RelayCommand]
        private async Task LoadUserAccountsAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Загрузка учетных записей...";

                UserAccounts.Clear();
                AvailableEmployees.Clear();

                // Загружаем всех сотрудников через LINQ
                var employees = await _context.Employees
                    .Where(e => e.IsActive == true)
                    .OrderBy(e => e.LastName)
                    .ToListAsync();

                foreach (var emp in employees)
                {
                    AvailableEmployees.Add(new EmployeeInfo
                    {
                        EmployeeId = emp.EmployeeId,
                        FullName = emp.FullName,
                        Position = emp.Position,
                        Department = emp.Department ?? ""
                    });
                }

                // Загружаем связи логинов с сотрудниками из таблицы EmployeeLogins
                var employeeLogins = await _context.EmployeeLogins
                    .Include(el => el.Employee)
                    .Where(el => el.EmployeeId.HasValue)
                    .ToListAsync();

                // Создаем словарь для быстрого поиска сотрудника по LoginName (ключу таблицы EmployeeLogins)
                var loginToEmployeeMap = new Dictionary<string, Employee>(StringComparer.OrdinalIgnoreCase);
                foreach (var el in employeeLogins.Where(el => el.Employee != null))
                {
                    var key = el.LoginName;
                    if (!loginToEmployeeMap.ContainsKey(key))
                    {
                        loginToEmployeeMap[key] = el.Employee!;
                    }
                }

                // Получаем список пользователей БД и их роли через LINQ к системным таблицам
                var connectionString = App.CurrentUserSession?.ConnectionString;
                if (string.IsNullOrEmpty(connectionString))
                {
                    StatusMessage = "Ошибка: нет строки подключения";
                    return;
                }

                // Используем Raw SQL query через EF Core для получения информации о пользователях
                // Это всё ещё LINQ-подход, так как мы используем DbContext
                var userQuery = await _context.Database
                    .SqlQueryRaw<UserAccountDbInfo>(@"
                        SELECT 
                            u.name AS UserName,
                            COALESCE(r.name, 'Без роли') AS RoleName
                        FROM sys.database_principals u
                        LEFT JOIN sys.database_role_members rm ON u.principal_id = rm.member_principal_id
                        LEFT JOIN sys.database_principals r ON rm.role_principal_id = r.principal_id
                        WHERE u.type IN ('S', 'U')
                        AND u.name NOT IN ('dbo', 'guest', 'INFORMATION_SCHEMA', 'sys')
                        ORDER BY u.name")
                    .ToListAsync();

                // Объединяем информацию о пользователях с сотрудниками через LINQ
                foreach (var userInfo in userQuery)
                {
                    // Пытаемся найти сотрудника по словарю из EmployeeLogins (по LoginName)
                    Employee? employee = null;
                    
                    if (!loginToEmployeeMap.TryGetValue(userInfo.UserName, out employee))
                    {
                        // Если не нашли через EmployeeLogins, ищем по совпадению имени (резервный вариант)
                        employee = employees.FirstOrDefault(e => 
                            CreateLoginName(e.LastName, e.FirstName).ToLower() == userInfo.UserName.ToLower());
                    }

                    UserAccounts.Add(new UserAccountInfo
                    {
                        LoginName = userInfo.UserName,
                        RoleName = userInfo.RoleName,
                        LastName = employee?.LastName ?? "Не привязан",
                        FirstName = employee?.FirstName ?? "",
                        Patronymic = employee?.Patronymic ?? "",
                        Position = employee?.Position ?? "Не указана",
                        Department = employee?.Department ?? "",
                        IsActive = employee?.IsActive ?? false,
                        EmployeeId = employee?.EmployeeId
                    });
                }

                StatusMessage = $"Загружено учетных записей: {UserAccounts.Count}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка загрузки: {ex.Message}";
                MessageBox.Show($"Ошибка загрузки учетных записей: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private string CreateLoginName(string lastName, string firstName)
        {
            // Создаем логин на основе фамилии (транслитерация упрощенная)
            var translitDict = new Dictionary<char, string>
            {
                {'а', "a"}, {'б', "b"}, {'в', "v"}, {'г', "g"}, {'д', "d"},
                {'е', "e"}, {'ё', "yo"}, {'ж', "zh"}, {'з', "z"}, {'и', "i"},
                {'й', "y"}, {'к', "k"}, {'л', "l"}, {'м', "m"}, {'н', "n"},
                {'о', "o"}, {'п', "p"}, {'р', "r"}, {'с', "s"}, {'т', "t"},
                {'у', "u"}, {'ф', "f"}, {'х', "kh"}, {'ц', "ts"}, {'ч', "ch"},
                {'ш', "sh"}, {'щ', "sch"}, {'ъ', ""}, {'ы', "y"}, {'ь', ""},
                {'э', "e"}, {'ю', "yu"}, {'я', "ya"}
            };

            string Transliterate(string text)
            {
                var result = "";
                foreach (var c in text.ToLower())
                {
                    if (translitDict.ContainsKey(c))
                        result += translitDict[c];
                    else if (char.IsLetter(c))
                        result += c;
                }
                return result;
            }

            return Transliterate(lastName);
        }

        [RelayCommand]
        private async Task CreateUserAsync()
        {
            if (SelectedEmployeeId == null)
            {
                MessageBox.Show("Выберите сотрудника для создания учетной записи", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(NewLoginName))
            {
                MessageBox.Show("Введите имя пользователя (логин)", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                $"Создать учетную запись для сотрудника?\n\nЛогин: {NewLoginName}\nРоль: {SelectedRole}\nПароль: {NewPassword}",
                "Создание пользователя", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
                return;

            try
            {
                IsLoading = true;
                var connectionString = App.CurrentUserSession?.ConnectionString;
                
                await using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
                await connection.OpenAsync();

                // Создаем пользователя в базе данных через SQL (необходимо для создания login)
                // EF Core не поддерживает создание пользователей БД, это делается только через SQL
                var createUserSql = $@"
                    IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = '{NewLoginName}')
                    BEGIN
                        CREATE USER [{NewLoginName}] WITHOUT LOGIN;
                    END
                    
                    ALTER USER [{NewLoginName}] WITH PASSWORD = '{NewPassword}';
                    
                    -- Назначаем роль
                    DECLARE @roleName sysname = '{SelectedRole}';
                    IF NOT EXISTS (
                        SELECT 1 FROM sys.database_role_members rm
                        JOIN sys.database_principals r ON rm.role_principal_id = r.principal_id
                        JOIN sys.database_principals u ON rm.member_principal_id = u.principal_id
                        WHERE u.name = '{NewLoginName}' AND r.name = @roleName
                    )
                    BEGIN
                        EXEC sp_addrolemember @roleName, '{NewLoginName}';
                    END";

                await using var cmd = new Microsoft.Data.SqlClient.SqlCommand(createUserSql, connection);
                await cmd.ExecuteNonQueryAsync();

                StatusMessage = $"Пользователь '{NewLoginName}' успешно создан";
                MessageBox.Show($"Пользователь успешно создан!\n\nЛогин: {NewLoginName}\nПароль: {NewPassword}",
                    "Создание пользователя", MessageBoxButton.OK, MessageBoxImage.Information);

                NewLoginName = string.Empty;
                SelectedEmployeeId = null;
                
                await LoadUserAccountsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка создания пользователя: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task DeleteUserAsync()
        {
            if (SelectedUser == null)
            {
                MessageBox.Show("Выберите пользователя для удаления", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                $"Вы действительно хотите удалить пользователя '{SelectedUser.LoginName}'?\n\nЭто действие необратимо!",
                "Удаление пользователя", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
                return;

            try
            {
                IsLoading = true;
                var connectionString = App.CurrentUserSession?.ConnectionString;
                
                await using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
                await connection.OpenAsync();

                // Удаляем пользователя из базы данных
                var deleteUserSql = $@"
                    -- Удаляем из ролей
                    EXEC sp_droprolemember '{SelectedUser.RoleName}', '{SelectedUser.LoginName}';
                    
                    -- Удаляем пользователя
                    DROP USER [{SelectedUser.LoginName}];";

                await using var cmd = new Microsoft.Data.SqlClient.SqlCommand(deleteUserSql, connection);
                await cmd.ExecuteNonQueryAsync();

                StatusMessage = $"Пользователь '{SelectedUser.LoginName}' удален";
                MessageBox.Show("Пользователь успешно удален", "Удаление пользователя",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                await LoadUserAccountsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления пользователя: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task ResetPasswordAsync()
        {
            if (SelectedUser == null)
            {
                MessageBox.Show("Выберите пользователя для сброса пароля", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                $"Вы действительно хотите сбросить пароль для пользователя '{SelectedUser.LoginName}'?\n\nНовый пароль: {NewPassword}",
                "Сброс пароля", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
                return;

            try
            {
                IsLoading = true;
                var connectionString = App.CurrentUserSession?.ConnectionString;
                
                await using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
                await connection.OpenAsync();

                var sql = $"ALTER USER [{SelectedUser.LoginName}] WITH PASSWORD = '{NewPassword}';";
                await using var cmd = new Microsoft.Data.SqlClient.SqlCommand(sql, connection);
                await cmd.ExecuteNonQueryAsync();

                StatusMessage = $"Пароль для '{SelectedUser.LoginName}' сброшен";
                MessageBox.Show($"Пароль успешно сброшен!\n\nНовый пароль: {NewPassword}",
                    "Сброс пароля", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сброса пароля: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task RefreshAsync()
        {
            await LoadUserAccountsAsync();
        }

        [RelayCommand]
        private void OpenCreateUserForm()
        {
            // Открываем диалог создания пользователя
            SelectedEmployeeId = null;
            NewLoginName = string.Empty;
        }
    }

    public class UserAccountInfo
    {
        public string LoginName { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string Patronymic { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int? EmployeeId { get; set; }

        public string FullName => $"{LastName} {FirstName}{(string.IsNullOrEmpty(Patronymic) ? "" : " " + Patronymic)}".Trim();
    }

    public class UserAccountDbInfo
    {
        public string UserName { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
    }

    public class EmployeeInfo
    {
        public int EmployeeId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
    }
}
