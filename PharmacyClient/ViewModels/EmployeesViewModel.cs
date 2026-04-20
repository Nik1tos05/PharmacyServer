using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using PharmacyClient.Data;
using PharmacyClient.Services;
using PharmacyServer.Models;

namespace PharmacyClient.ViewModels
{
    public partial class EmployeesViewModel : ObservableObject
    {
        private readonly PharmacyClient.Data.PharmacyDbContext _context;
        private readonly SqlServerUserManagementService _userService;

        [ObservableProperty]
        private ObservableCollection<Employee> _employees = new();

        [ObservableProperty]
        private Employee? _selectedEmployee;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        // Фильтры
        [ObservableProperty]
        private string _filterDepartment = "Все";

        [ObservableProperty]
        private ObservableCollection<string> _departments = new();

        public EmployeesViewModel()
        {
            _context = new PharmacyClient.Data.PharmacyDbContext();
            Departments.Add("Все");
            
            // Инициализируем сервис управления пользователями
            var connectionString = _context.Database.GetConnectionString();
            _userService = new SqlServerUserManagementService(connectionString!);
        }

        [RelayCommand]
        private async Task LoadEmployeesAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Загрузка данных...";

                var query = _context.Employees.AsQueryable();

                // Применяем поиск
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    query = query.Where(e => 
                        e.LastName.Contains(SearchText) || 
                        e.FirstName.Contains(SearchText) ||
                        (e.Patronymic != null && e.Patronymic.Contains(SearchText)) ||
                        e.Position.Contains(SearchText));
                }

                // Применяем фильтр по отделу
                if (!string.IsNullOrEmpty(FilterDepartment) && FilterDepartment != "Все")
                {
                    query = query.Where(e => e.Department == FilterDepartment);
                }

                var employees = await query.OrderBy(e => e.LastName).ThenBy(e => e.FirstName).ToListAsync();

                Employees.Clear();
                foreach (var emp in employees)
                {
                    Employees.Add(emp);
                }

                // Обновляем список отделов
                await LoadDepartmentsAsync();

                StatusMessage = $"Загружено сотрудников: {Employees.Count}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка загрузки: {ex.Message}";
                MessageBox.Show($"Ошибка загрузки сотрудников: {ex.Message}", "Ошибка", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadDepartmentsAsync()
        {
            try
            {
                var departments = await _context.Employees
                    .Where(e => e.Department != null)
                    .Select(e => e.Department!)
                    .Distinct()
                    .OrderBy(d => d)
                    .ToListAsync();

                Departments.Clear();
                Departments.Add("Все");
                foreach (var dept in departments)
                {
                    Departments.Add(dept);
                }
            }
            catch
            {
                // Игнорируем ошибки при загрузке отделов
            }
        }

        [RelayCommand]
        private void Search()
        {
            LoadEmployeesCommand.Execute(null);
        }

        [RelayCommand]
        private void ClearSearch()
        {
            SearchText = string.Empty;
            FilterDepartment = "Все";
            LoadEmployeesCommand.Execute(null);
        }

        partial void OnFilterDepartmentChanged(string value)
        {
            LoadEmployeesCommand.Execute(null);
        }

        [RelayCommand]
        private async Task AddEmployeeAsync()
        {
            try
            {
                var newEmployee = new Employee
                {
                    LastName = "Новая Фамилия",
                    FirstName = "Имя",
                    Patronymic = "Отчество",
                    Position = "Должность",
                    Department = "Отдел",
                    HireDate = DateOnly.FromDateTime(DateTime.Today),
                    Phone = "+7(000)000-00-00",
                    Email = "email@example.com",
                    PassportSeries = "0000",
                    PassportNumber = "000000",
                    Salary = 0,
                    IsManager = false,
                    CanSignDocuments = false,
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    ModifiedDate = DateTime.Now
                };

                _context.Employees.Add(newEmployee);
                await _context.SaveChangesAsync();

                // Автоматически создаем учетную запись SQL Server для нового сотрудника
                try
                {
                    await _userService.CreateUserAsync(newEmployee, "12345678");
                    StatusMessage = $"Сотрудник добавлен. Логин для входа: {newEmployee.LastName.ToLower()}";
                }
                catch (Exception sqlEx)
                {
                    StatusMessage = $"Сотрудник добавлен, но не удалось создать учетную запись: {sqlEx.Message}";
                    MessageBox.Show(
                        $"Сотрудник добавлен в базу данных, но не удалось автоматически создать учетную запись SQL Server.\n\n" +
                        $"Ошибка: {sqlEx.Message}\n\n" +
                        $"Убедитесь, что вы вошли под учетной записью с правами sysadmin.",
                        "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                await LoadEmployeesAsync();
                
                MessageBox.Show(
                    $"Сотрудник успешно добавлен!\n\n" +
                    $"Логин для входа: {newEmployee.LastName.ToLower()}\n" +
                    $"Пароль: 12345678\n\n" +
                    $"Учетная запись SQL Server создана автоматически.",
                    "Добавление сотрудника", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка добавления: {ex.Message}", "Ошибка", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task EditEmployeeAsync()
        {
            if (SelectedEmployee == null)
            {
                MessageBox.Show("Выберите сотрудника для редактирования", "Предупреждение", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Обновляем роль пользователя в SQL Server при изменении должности
                try
                {
                    await _userService.UpdateUserRoleAsync(SelectedEmployee);
                }
                catch (Exception sqlEx)
                {
                    MessageBox.Show(
                        $"Не удалось обновить роль в SQL Server.\n" +
                        $"Ошибка: {sqlEx.Message}",
                        "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                // Обновляем ModifiedDate
                SelectedEmployee.ModifiedDate = DateTime.Now;
                await _context.SaveChangesAsync();

                StatusMessage = "Сотрудник обновлен";
                await LoadEmployeesAsync();
                
                MessageBox.Show("Данные сотрудника обновлены!\n\nРоль в SQL Server также обновлена.", 
                    "Редактирование", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления: {ex.Message}", "Ошибка", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task ResetPasswordAsync()
        {
            if (SelectedEmployee == null)
            {
                MessageBox.Show("Выберите сотрудника для сброса пароля", "Предупреждение", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Вы действительно хотите сбросить пароль для сотрудника {SelectedEmployee.FullName}?\n\n" +
                $"Новый пароль будет: 12345678",
                "Сброс пароля", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                await _userService.ResetPasswordAsync(SelectedEmployee, "12345678");
                
                MessageBox.Show(
                    $"Пароль для сотрудника {SelectedEmployee.FullName} успешно сброшен!\n\n" +
                    $"Новый пароль: 12345678",
                    "Сброс пароля", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сброса пароля: {ex.Message}", "Ошибка", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task DeleteEmployeeAsync()
        {
            if (SelectedEmployee == null)
            {
                MessageBox.Show("Выберите сотрудника для удаления", "Предупреждение", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Вы действительно хотите удалить сотрудника {SelectedEmployee.FullName}?\n\n" +
                $"Внимание: это также удалит учетную запись SQL Server для этого сотрудника.",
                "Удаление сотрудника", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                // Сначала удаляем учетную запись SQL Server
                try
                {
                    await _userService.DeleteUserAsync(SelectedEmployee);
                }
                catch (Exception sqlEx)
                {
                    MessageBox.Show(
                        $"Не удалось автоматически удалить учетную запись SQL Server.\n" +
                        $"Ошибка: {sqlEx.Message}\n\n" +
                        $"Сотрудник будет удален из базы данных.",
                        "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                // Удаляем сотрудника из базы данных
                _context.Employees.Remove(SelectedEmployee);
                await _context.SaveChangesAsync();

                StatusMessage = "Сотрудник удален";
                SelectedEmployee = null;
                await LoadEmployeesAsync();
                
                MessageBox.Show("Сотрудник успешно удален!\n\nУчетная запись SQL Server также удалена.", 
                    "Удаление сотрудника", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task RefreshAsync()
        {
            await LoadEmployeesAsync();
        }
    }
}
