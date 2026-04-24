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
        private bool _isAddingUser;

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

                // Получаем список пользователей БД и их роли через LINQ к системным таблицам
                var connectionString = App.CurrentUserSession?.ConnectionString;
                if (string.IsNullOrEmpty(connectionString))
                {
                    StatusMessage = "Ошибка: нет строки подключения";
                    return;
                }

                // Используем Raw SQL query через EF Core для получения информации о пользователях
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

                // Создаем информацию о пользователях
                foreach (var userInfo in userQuery)
                {
                    UserAccounts.Add(new UserAccountInfo
                    {
                        LoginName = userInfo.UserName,
                        RoleName = userInfo.RoleName,
                        LastName = "",
                        FirstName = "",
                        Patronymic = "",
                        Position = "",
                        Department = "",
                        IsActive = true,
                        EmployeeId = null
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
        private void AddUser()
        {
            IsAddingUser = true;
            NewLoginName = string.Empty;
            SelectedRole = "Pharmacy_Staff";
        }

        [RelayCommand]
        private void CancelAddUser()
        {
            IsAddingUser = false;
            NewLoginName = string.Empty;
        }

        [RelayCommand]
        private async Task CreateUserAsync()
        {
            if (string.IsNullOrWhiteSpace(NewLoginName))
            {
                MessageBox.Show("Введите имя пользователя (логин)", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                $"Создать учетную запись?\n\nЛогин: {NewLoginName}\nРоль: {SelectedRole}\nПароль: {NewPassword}",
                "Создание пользователя", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
                return;

            try
            {
                IsLoading = true;
                var connectionString = App.CurrentUserSession?.ConnectionString;
                
                // Создаем строку подключения к master для создания LOGIN
                var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString)
                {
                    InitialCatalog = "master"
                };
                var masterConnectionString = builder.ConnectionString;

                await using var masterConnection = new Microsoft.Data.SqlClient.SqlConnection(masterConnectionString);
                await masterConnection.OpenAsync();

                // 1. Сначала создаем LOGIN на уровне сервера (если не существует)
                var checkLoginSql = $"SELECT COUNT(*) FROM sys.server_principals WHERE name = @LoginName";
                await using (var checkCmd = new Microsoft.Data.SqlClient.SqlCommand(checkLoginSql, masterConnection))
                {
                    checkCmd.Parameters.AddWithValue("@LoginName", NewLoginName);
                    var exists = (int)await checkCmd.ExecuteScalarAsync();

                    if (exists == 0)
                    {
                        // Используем EXECUTE AS SELF для обхода ограничений прав
                        var createLoginSql = $@"
                            DECLARE @sql NVARCHAR(MAX);
                            SET @sql = N'CREATE LOGIN [{NewLoginName}] WITH PASSWORD = ''{NewPassword}'', CHECK_POLICY = OFF, DEFAULT_DATABASE = [PharmacyDB]';
                            EXEC(@sql);";
                        
                        await using (var createCmd = new Microsoft.Data.SqlClient.SqlCommand(createLoginSql, masterConnection))
                        {
                            await createCmd.ExecuteNonQueryAsync();
                        }
                    }
                    else
                    {
                        // Обновляем пароль и БД по умолчанию
                        var updateLoginSql = $@"
                            DECLARE @sql NVARCHAR(MAX);
                            SET @sql = N'ALTER LOGIN [{NewLoginName}] WITH PASSWORD = ''{NewPassword}'', DEFAULT_DATABASE = [PharmacyDB]';
                            EXEC(@sql);";
                        
                        await using (var updateCmd = new Microsoft.Data.SqlClient.SqlCommand(updateLoginSql, masterConnection))
                        {
                            await updateCmd.ExecuteNonQueryAsync();
                        }
                    }
                }

                // 2. Переключаемся на базу данных PharmacyDB и создаем USER
                await using var dbConnection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
                await dbConnection.OpenAsync();

                var checkUserSql = $"SELECT COUNT(*) FROM sys.database_principals WHERE name = @LoginName";
                await using (var checkCmd = new Microsoft.Data.SqlClient.SqlCommand(checkUserSql, dbConnection))
                {
                    checkCmd.Parameters.AddWithValue("@LoginName", NewLoginName);
                    var exists = (int)await checkCmd.ExecuteScalarAsync();

                    if (exists == 0)
                    {
                        var createUserSql = $"CREATE USER [{NewLoginName}] FOR LOGIN [{NewLoginName}]";
                        await using (var createCmd = new Microsoft.Data.SqlClient.SqlCommand(createUserSql, dbConnection))
                        {
                            await createCmd.ExecuteNonQueryAsync();
                        }
                    }
                }

                // 3. Назначаем роль
                var assignRoleSql = $@"
                    IF NOT EXISTS (
                        SELECT 1 FROM sys.database_role_members rm
                        JOIN sys.database_principals r ON rm.role_principal_id = r.principal_id
                        JOIN sys.database_principals u ON rm.member_principal_id = u.principal_id
                        WHERE u.name = @LoginName AND r.name = @RoleName
                    )
                    BEGIN
                        ALTER ROLE [{SelectedRole}] ADD MEMBER [{NewLoginName}];
                    END";

                await using (var assignCmd = new Microsoft.Data.SqlClient.SqlCommand(assignRoleSql, dbConnection))
                {
                    assignCmd.Parameters.AddWithValue("@LoginName", NewLoginName);
                    assignCmd.Parameters.AddWithValue("@RoleName", SelectedRole);
                    await assignCmd.ExecuteNonQueryAsync();
                }

                StatusMessage = $"Пользователь '{NewLoginName}' успешно создан";
                MessageBox.Show($"Пользователь успешно создан!\n\nЛогин: {NewLoginName}\nПароль: {NewPassword}",
                    "Создание пользователя", MessageBoxButton.OK, MessageBoxImage.Information);

                NewLoginName = string.Empty;
                IsAddingUser = false;
                
                await LoadUserAccountsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка создания пользователя: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusMessage = $"Ошибка: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand(CanExecute = nameof(CanDeleteUser))]
        private async Task DeleteUserAsync()
        {
            if (SelectedUser == null)
            {
                MessageBox.Show("Выберите пользователя для удаления", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                $"Вы действительно хотите удалить пользователя '{SelectedUser.LoginName}'?\\n\\nЭто действие необратимо!",
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

        private bool CanDeleteUser()
        {
            return SelectedUser != null;
        }

        [RelayCommand(CanExecute = nameof(CanResetPassword))]
        private async Task ResetPasswordAsync()
        {
            if (SelectedUser == null)
            {
                MessageBox.Show("Выберите пользователя для сброса пароля", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                $"Вы действительно хотите сбросить пароль для пользователя '{SelectedUser.LoginName}'?\\n\\nНовый пароль: {NewPassword}",
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
                MessageBox.Show($"Пароль успешно сброшен!\\n\\nНовый пароль: {NewPassword}",
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

        private bool CanResetPassword()
        {
            return SelectedUser != null;
        }

        [RelayCommand]
        private async Task SaveEditedUserAsync(object? parameter)
        {
            if (parameter is not UserAccountInfo user)
                return;

            try
            {
                IsLoading = true;
                var connectionString = App.CurrentUserSession?.ConnectionString;

                await using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
                await connection.OpenAsync();

                // Обновляем роль пользователя
                var updateRoleSql = $@"
                    -- Удаляем из текущей роли
                    EXEC sp_droprolemember '{SelectedRole}', '{user.LoginName}';
                    
                    -- Добавляем в новую роль
                    EXEC sp_addrolemember '{user.RoleName}', '{user.LoginName}';";

                await using var cmd = new Microsoft.Data.SqlClient.SqlCommand(updateRoleSql, connection);
                await cmd.ExecuteNonQueryAsync();

                StatusMessage = $"Пользователь '{user.LoginName}' обновлен";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления пользователя: {ex.Message}", "Ошибка",
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

        partial void OnSelectedUserChanged(UserAccountInfo? value)
        {
            // Обновляем состояние кнопок при изменении выбранного пользователя
            DeleteUserCommand.NotifyCanExecuteChanged();
            ResetPasswordCommand.NotifyCanExecuteChanged();
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
}
