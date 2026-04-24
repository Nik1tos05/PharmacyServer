using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using PharmacyClient.Data;
using PharmacyClient.Models;

namespace PharmacyClient.ViewModels
{
    public partial class EmployeesViewModel : ObservableObject
    {
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
            Departments.Add("Все");
        }

        [RelayCommand]
        public async Task LoadEmployeesAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Загрузка данных...";

                await using var context = new PharmacyDbContext();
                var query = context.Employees.AsNoTracking().AsQueryable();

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
                await using var context = new PharmacyDbContext();
                var departments = await context.Employees
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
        private async Task RefreshAsync()
        {
            await LoadEmployeesAsync();
        }
    }
}
