using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Data.SqlClient;
using PharmacyClient.Data;

namespace PharmacyClient.ViewModels
{
    public partial class AdministrationViewModel : ObservableObject
    {
        private readonly PharmacyClient.Data.PharmacyDbContext _context;

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

        public AdministrationViewModel()
        {
            _context = new PharmacyClient.Data.PharmacyDbContext();
        }

        [RelayCommand]
        private async Task LoadUserAccountsAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Загрузка учетных записей...";

                UserAccounts.Clear();

                // Получаем список пользователей БД и их роли
                var connectionString = App.CurrentUserSession?.ConnectionString;
                if (string.IsNullOrEmpty(connectionString))
                {
                    StatusMessage = "Ошибка: нет строки подключения";
                    return;
                }

                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT 
                        u.name AS UserName,
                        COALESCE(r.name, 'Без роли') AS RoleName,
                        e.LastName,
                        e.FirstName,
                        e.Position,
                        e.IsActive
                    FROM sys.database_principals u
                    LEFT JOIN sys.database_role_members rm ON u.principal_id = rm.member_principal_id
                    LEFT JOIN sys.database_principals r ON rm.role_principal_id = r.principal_id
                    LEFT JOIN dbo.Employees e ON LOWER(e.LastName) IN (
                        'ivanov', 'petrova', 'sidorov', 'kuznetsova', 'smirnov',
                        'morozova', 'volkov', 'zaytseva', 'novikov', 'lebedeva'
                    ) AND LOWER(u.name) = CASE LOWER(e.LastName)
                        WHEN 'иванов' THEN 'ivanov'
                        WHEN 'петрова' THEN 'petrova'
                        WHEN 'сидоров' THEN 'sidorov'
                        WHEN 'кузнецова' THEN 'kuznetsova'
                        WHEN 'смирнов' THEN 'smirnov'
                        WHEN 'морозова' THEN 'morozova'
                        WHEN 'волков' THEN 'volkov'
                        WHEN 'зайцева' THEN 'zaytseva'
                        WHEN 'новиков' THEN 'novikov'
                        WHEN 'лебедева' THEN 'lebedeva'
                    END
                    WHERE u.type IN ('S', 'U')
                    AND u.name NOT IN ('dbo', 'guest', 'INFORMATION_SCHEMA', 'sys')
                    ORDER BY u.name;";

                await using var cmd = new SqlCommand(query, connection);
                await using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    UserAccounts.Add(new UserAccountInfo
                    {
                        LoginName = reader.GetString(0),
                        RoleName = reader.GetString(1),
                        LastName = reader.IsDBNull(2) ? "Не привязан" : reader.GetString(2),
                        FirstName = reader.IsDBNull(3) ? "" : reader.GetString(3),
                        Position = reader.IsDBNull(4) ? "Не указана" : reader.GetString(4),
                        IsActive = reader.IsDBNull(5) || reader.GetBoolean(5)
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

        [RelayCommand]
        private void OpenCreateUserScript()
        {
            var message = @"Для создания нового пользователя выполните следующие шаги:

1. Добавьте сотрудника через вкладку 'Сотрудники'
2. Запомните EmployeeID нового сотрудника
3. Откройте файл create_employee_login.sql в SSMS
4. Измените параметры скрипта:
   - @EmployeeID = [ID нового сотрудника]
   - @LoginName = '[желаемый логин на латинице]'
   - @RoleName = '[нужная роль]'
5. Выполните скрипт в SSMS

После этого сотрудник сможет войти в систему под новым логином.";

            MessageBox.Show(message, "Создание пользователя", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        [RelayCommand]
        private void OpenDeleteUserScript()
        {
            if (SelectedUser == null)
            {
                MessageBox.Show("Выберите пользователя для удаления", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var message = $@"Для удаления пользователя '{SelectedUser.LoginName}' выполните:

1. Откройте файл delete_employee_login.sql в SSMS
2. Измените параметр:
   - @LoginName = '{SelectedUser.LoginName}'
3. Выполните скрипт в SSMS
4. Удалите сотрудника через вкладку 'Сотрудники' (если еще не удалили)

Внимание: это действие необратимо!";

            var result = MessageBox.Show(message, "Удаление пользователя",
                MessageBoxButton.OKCancel, MessageBoxImage.Warning);
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
                
                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var sql = $"ALTER LOGIN [{SelectedUser.LoginName}] WITH PASSWORD = '{NewPassword}';";
                await using var cmd = new SqlCommand(sql, connection);
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
    }

    public class UserAccountInfo
    {
        public string LoginName { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public bool IsActive { get; set; }

        public string FullName => $"{LastName} {FirstName}".Trim();
    }
}
