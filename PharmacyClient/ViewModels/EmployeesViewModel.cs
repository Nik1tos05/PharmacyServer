using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using PharmacyClient.Data;
using PharmacyServer.Models;

namespace PharmacyClient.ViewModels
{
    public partial class EmployeesViewModel : ObservableObject
    {
        private readonly PharmacyDbContext _context;

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
            _context = new PharmacyDbContext();
            Departments.Add("Все");
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

                StatusMessage = "Сотрудник добавлен";
                await LoadEmployeesAsync();
                
                MessageBox.Show("Сотрудник успешно добавлен!\n\nПосле добавления сотрудника необходимо создать для него учетную запись в SQL Server через скрипт.", 
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
                // В реальном приложении здесь должно открываться окно редактирования
                // Для простоты просто обновляем ModifiedDate
                SelectedEmployee.ModifiedDate = DateTime.Now;
                await _context.SaveChangesAsync();

                StatusMessage = "Сотрудник обновлен";
                await LoadEmployeesAsync();
                
                MessageBox.Show("Данные сотрудника обновлены!", "Редактирование", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления: {ex.Message}", "Ошибка", 
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
                $"Внимание: это не удалит учетную запись в SQL Server. Это нужно сделать отдельно.",
                "Удаление сотрудника", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                _context.Employees.Remove(SelectedEmployee);
                await _context.SaveChangesAsync();

                StatusMessage = "Сотрудник удален";
                SelectedEmployee = null;
                await LoadEmployeesAsync();
                
                MessageBox.Show("Сотрудник успешно удален!\n\nНе забудьте удалить учетную запись в SQL Server через скрипт.", 
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
