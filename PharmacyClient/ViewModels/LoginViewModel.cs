using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using PharmacyClient.Data;
using PharmacyServer.Models;

namespace PharmacyClient.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly PharmacyDbContext _context;

        [ObservableProperty]
        private string _lastName = string.Empty;

        [ObservableProperty]
        private string _passportNumber = string.Empty;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private ObservableCollection<Employee> _employees = new();

        [ObservableProperty]
        private Employee? _selectedEmployee;

        public LoginViewModel()
        {
            _context = new PharmacyDbContext();
        }

        [RelayCommand]
        private async Task LoadEmployeesAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                var employees = await _context.Employees
                    .Where(e => e.IsActive == true)
                    .OrderBy(e => e.LastName)
                    .ToListAsync();

                Employees = new ObservableCollection<Employee>(employees);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка загрузки сотрудников: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void Login()
        {
            if (SelectedEmployee == null)
            {
                ErrorMessage = "Выберите сотрудника из списка";
                return;
            }

            // Create user session
            var session = new UserSession
            {
                EmployeeId = SelectedEmployee.EmployeeId,
                LastName = SelectedEmployee.LastName,
                FirstName = SelectedEmployee.FirstName,
                Patronymic = SelectedEmployee.Patronymic,
                Position = SelectedEmployee.Position,
                Department = SelectedEmployee.Department,
                IsManager = SelectedEmployee.IsManager ?? false,
                CanSignDocuments = SelectedEmployee.CanSignDocuments ?? false
            };

            App.SetCurrentUserSession(session);

            // Open main window based on role
            var mainWindow = new MainWindow();
            mainWindow.Show();

            // Close login window
            Application.Current?.MainWindow?.Close();
            Application.Current!.MainWindow = mainWindow;
        }

        [RelayCommand]
        private void Exit()
        {
            Application.Current?.Shutdown();
        }
    }
}
