using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PharmacyClient.Data;
using PharmacyServer.Models;

namespace PharmacyClient.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        // Строка подключения по умолчанию (будет использоваться для проверки логина/пароля)
        private const string DefaultConnectionString = "Server=localhost;Database=PharmacyDB;Trusted_Connection=True;TrustServerCertificate=True;";

        [ObservableProperty]
        private string _login = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _rememberMe;

        public LoginViewModel()
        {
        }

        partial void OnPasswordChanged(string value)
        {
            ErrorMessage = string.Empty;
        }

        partial void OnLoginChanged(string value)
        {
            ErrorMessage = string.Empty;
        }

        [RelayCommand]
        private async Task LoginAsync()
        {
            if (string.IsNullOrWhiteSpace(Login))
            {
                ErrorMessage = "Введите логин";
                return;
            }

            if (string.IsNullOrEmpty(Password))
            {
                ErrorMessage = "Введите пароль";
                return;
            }

            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                // Формируем строку подключения с указанным логином и паролем
                var connectionString = $"Server=localhost;Database=PharmacyDB;User ID={Login};Password={Password};TrustServerCertificate=True;";

                // Пробуем подключиться к БД с этими учетными данными
                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // Получаем информацию о текущем пользователе из БД
                // Ищем сотрудника по фамилии (логин у нас = фамилия в нижнем регистре)
                var employeeQuery = @"
                    SELECT TOP 1 
                        e.EmployeeID, e.LastName, e.FirstName, e.Patronymic, e.Position, e.Department,
                        e.IsManager, e.CanSignDocuments, e.IsActive
                    FROM dbo.Employees e
                    WHERE LOWER(e.LastName) = LOWER(@Login) AND e.IsActive = 1";

                Employee? employee = null;

                await using (var cmd = new SqlCommand(employeeQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@Login", Login);
                    
                    await using var reader = await cmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        employee = new Employee
                        {
                            EmployeeId = reader.GetInt32(0),
                            LastName = reader.GetString(1),
                            FirstName = reader.GetString(2),
                            Patronymic = reader.IsDBNull(3) ? null : reader.GetString(3),
                            Position = reader.GetString(4),
                            Department = reader.IsDBNull(5) ? null : reader.GetString(5),
                            IsManager = reader.IsDBNull(6) ? false : reader.GetBoolean(6),
                            CanSignDocuments = reader.IsDBNull(7) ? false : reader.GetBoolean(7),
                            IsActive = reader.IsDBNull(8) ? false : reader.GetBoolean(8)
                        };
                    }
                }

                if (employee == null)
                {
                    // Если не нашли по имени пользователя вEmployees, пробуем найти по роли
                    // или используем данные по умолчанию для входа
                    ErrorMessage = "Пользователь не найден в базе сотрудников. Проверьте логин.";
                    IsLoading = false;
                    return;
                }

                // Создаем сессию пользователя
                var session = new UserSession
                {
                    EmployeeId = employee.EmployeeId,
                    LastName = employee.LastName,
                    FirstName = employee.FirstName,
                    Patronymic = employee.Patronymic,
                    Position = employee.Position,
                    Department = employee.Department,
                    IsManager = employee.IsManager ?? false,
                    CanSignDocuments = employee.CanSignDocuments ?? false,
                    ConnectionString = connectionString // Сохраняем строку подключения для сессии
                };

                App.SetCurrentUserSession(session);

                // Открываем главное окно
                var mainWindow = new MainWindow();
                mainWindow.Show();

                // Закрываем окно входа
                Application.Current?.MainWindow?.Close();
                Application.Current!.MainWindow = mainWindow;
            }
            catch (SqlException sqlEx)
            {
                ErrorMessage = $"Ошибка входа: {sqlEx.Message}";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка входа: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void Exit()
        {
            Application.Current?.Shutdown();
        }
    }
}
