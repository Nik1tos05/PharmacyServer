using System.Collections.ObjectModel;
using System.IO;
using System.IO.IsolatedStorage;
using System.Text;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PharmacyClient.Data;
using PharmacyClient.Models;

namespace PharmacyClient.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        // Строка подключения по умолчанию (будет использоваться для проверки логина/пароля)
        private const string DefaultConnectionString = "Server=localhost;Database=PharmacyDB;Trusted_Connection=True;TrustServerCertificate=True;";
        private const string CredentialsFileName = "credentials.dat";

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
            LoadSavedCredentials();
        }

        partial void OnPasswordChanged(string value)
        {
            ErrorMessage = string.Empty;
        }

        partial void OnLoginChanged(string value)
        {
            ErrorMessage = string.Empty;
        }

        private void LoadSavedCredentials()
        {
            try
            {
                using var store = IsolatedStorageFile.GetUserStoreForAssembly();
                if (store.FileExists(CredentialsFileName))
                {
                    await using var stream = store.OpenFile(CredentialsFileName, FileMode.Open);
                    using var reader = new StreamReader(stream, Encoding.UTF8);
                    
                    var login = reader.ReadLine();
                    var password = reader.ReadLine();
                    var rememberMe = reader.ReadLine();

                    if (!string.IsNullOrEmpty(login) && !string.IsNullOrEmpty(password))
                    {
                        Login = login;
                        Password = password;
                        RememberMe = rememberMe == "true";
                    }
                }
            }
            catch
            {
                // Если не удалось загрузить, просто оставляем поля пустыми
            }
        }

        private void SaveCredentials()
        {
            try
            {
                using var store = IsolatedStorageFile.GetUserStoreForAssembly();
                await using var stream = store.CreateFile(CredentialsFileName);
                using var writer = new StreamWriter(stream, Encoding.UTF8);

                if (RememberMe)
                {
                    writer.WriteLine(Login);
                    writer.WriteLine(Password);
                    writer.WriteLine("true");
                }
                else
                {
                    // Если галочка снята, удаляем файл с данными
                    if (store.FileExists(CredentialsFileName))
                    {
                        store.DeleteFile(CredentialsFileName);
                    }
                }
            }
            catch
            {
                // Если не удалось сохранить, просто игнорируем ошибку
            }
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

                // Словарь соответствия: Логин (латиница) -> Фамилия (кириллица)
                var loginMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "ivanov", "Иванов" },
                    { "petrova", "Петрова" },
                    { "sidorov", "Сидоров" },
                    { "kuznetsova", "Кузнецова" },
                    { "smirnov", "Смирнов" },
                    { "morozova", "Морозова" },
                    { "volkov", "Волков" },
                    { "zaytseva", "Зайцева" },
                    { "novikov", "Новиков" },
                    { "lebedeva", "Лебедева" }
                };

                // Преобразуем логин в фамилию
                if (!loginMap.TryGetValue(Login, out string? lastNameCyrillic))
                {
                    ErrorMessage = $"Неизвестный логин '{Login}'. Проверьте правильность ввода или обратитесь к администратору.";
                    IsLoading = false;
                    return;
                }

                // Получаем информацию о текущем пользователе из БД
                // Ищем сотрудника по фамилии (кириллица)
                var employeeQuery = @"
                    SELECT TOP 1 
                        e.EmployeeID, e.LastName, e.FirstName, e.Patronymic, e.Position, e.Department,
                        e.IsManager, e.CanSignDocuments, e.IsActive
                    FROM dbo.Employees e
                    WHERE e.LastName = @LastName AND e.IsActive = 1";

                Employee? employee = null;

                await using (var cmd = new SqlCommand(employeeQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@LastName", lastNameCyrillic);
                    
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

                // Сохраняем учетные данные, если выбрана опция "Запомнить меня"
                SaveCredentials();

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
