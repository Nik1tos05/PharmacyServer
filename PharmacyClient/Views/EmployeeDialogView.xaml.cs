using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using PharmacyClient.Data;
using PharmacyClient.Services;
using PharmacyClient.Models;

namespace PharmacyClient.Views
{
    /// <summary>
    /// Логика взаимодействия для EmployeeDialogView.xaml
    /// </summary>
    public partial class EmployeeDialogView : Window
    {
        public EmployeeDialogView(Employee? employee = null, ObservableCollection<string>? departments = null)
        {
            InitializeComponent();
            
            var viewModel = new EmployeeDialogViewModel(employee, departments);
            viewModel.RequestClose += () => Close();
            DataContext = viewModel;
        }
    }

    public partial class EmployeeDialogViewModel : ObservableValidator, IDisposable
    {
        private readonly SqlServerUserManagementService _userService;
        private readonly Employee? _originalEmployee;
        
        public event Action? RequestClose;

        [ObservableProperty]
        private bool _isEditMode;

        [ObservableProperty]
        private string _lastName = string.Empty;

        [ObservableProperty]
        private string _firstName = string.Empty;

        [ObservableProperty]
        private string? _patronymic;

        [ObservableProperty]
        private string _position = string.Empty;

        [ObservableProperty]
        private string? _department;

        [ObservableProperty]
        private DateOnly _hireDate = DateOnly.FromDateTime(DateTime.Today);

        [ObservableProperty]
        private string? _phone;

        [ObservableProperty]
        private string? _email;

        [ObservableProperty]
        private string? _passportSeries;

        [ObservableProperty]
        private string? _passportNumber;

        [ObservableProperty]
        private decimal? _salary;

        [ObservableProperty]
        private bool _isManager;

        [ObservableProperty]
        private bool _canSignDocuments;

        [ObservableProperty]
        private bool _isActive = true;

        [ObservableProperty]
        private ObservableCollection<string> _departments = new();

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public EmployeeDialogViewModel(Employee? employee = null, ObservableCollection<string>? departments = null)
        {
            var connectionString = App.CurrentUserSession?.ConnectionString ?? 
                                   "Server=localhost;Database=PharmacyDB;Trusted_Connection=True;TrustServerCertificate=True;";
            _userService = new SqlServerUserManagementService(connectionString);

            if (departments != null)
            {
                Departments = departments;
            }
            else
            {
                LoadDepartments();
            }

            if (employee != null)
            {
                IsEditMode = true;
                _originalEmployee = employee;
                
                LastName = employee.LastName;
                FirstName = employee.FirstName;
                Patronymic = employee.Patronymic;
                Position = employee.Position;
                Department = employee.Department;
                HireDate = employee.HireDate;
                Phone = employee.Phone;
                Email = employee.Email;
                PassportSeries = employee.PassportSeries;
                PassportNumber = employee.PassportNumber;
                Salary = employee.Salary;
                IsManager = employee.IsManager ?? false;
                CanSignDocuments = employee.CanSignDocuments ?? false;
                IsActive = employee.IsActive ?? true;
            }
            else
            {
                IsEditMode = false;
                IsActive = true;
            }

            SaveCommand = new RelayCommand(Save, CanSave);
            CancelCommand = new RelayCommand(Cancel);
        }

        private void LoadDepartments()
        {
            try
            {
                using var context = new PharmacyDbContext();
                var depts = context.Employees
                    .Where(e => e.Department != null)
                    .Select(e => e.Department!)
                    .Distinct()
                    .OrderBy(d => d)
                    .ToList();

                Departments.Clear();
                foreach (var dept in depts)
                {
                    Departments.Add(dept);
                }
            }
            catch
            {
                // Игнорируем ошибки
            }
        }

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(LastName) && 
                   !string.IsNullOrWhiteSpace(FirstName) && 
                   !string.IsNullOrWhiteSpace(Position);
        }

        private async void Save()
        {
            try
            {
                ValidateAllProperties();
                
                if (HasErrors)
                {
                    var errors = new List<string>();
                    foreach (var validationResult in GetErrors())
                    {
                        foreach (var memberName in validationResult.MemberNames)
                        {
                            foreach (var validationError in GetErrors(memberName))
                            {
                                if (!string.IsNullOrEmpty(validationError.ErrorMessage))
                                {
                                    errors.Add(validationError.ErrorMessage);
                                }
                            }
                        }
                    }

                    if (errors.Any())
                    {
                        MessageBox.Show($"Пожалуйста, исправьте ошибки:\n{string.Join("\n", errors)}", 
                            "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                if (IsEditMode && _originalEmployee != null)
                {
                    // Редактирование
                    await using var context = new PharmacyDbContext();
                    var employeeToUpdate = await context.Employees.FindAsync(_originalEmployee.EmployeeId);
                    if (employeeToUpdate != null)
                    {
                        employeeToUpdate.LastName = LastName;
                        employeeToUpdate.FirstName = FirstName;
                        employeeToUpdate.Patronymic = Patronymic;
                        employeeToUpdate.Position = Position;
                        employeeToUpdate.Department = Department;
                        employeeToUpdate.HireDate = HireDate;
                        employeeToUpdate.Phone = Phone;
                        employeeToUpdate.Email = Email;
                        employeeToUpdate.PassportSeries = PassportSeries;
                        employeeToUpdate.PassportNumber = PassportNumber;
                        employeeToUpdate.Salary = Salary;
                        employeeToUpdate.IsManager = IsManager;
                        employeeToUpdate.CanSignDocuments = CanSignDocuments;
                        employeeToUpdate.IsActive = IsActive;
                        employeeToUpdate.ModifiedDate = DateTime.Now;

                        await context.SaveChangesAsync();

                        // Обновляем роль в SQL Server
                        try
                        {
                            await _userService.UpdateUserRoleAsync(employeeToUpdate);
                        }
                        catch (Exception sqlEx)
                        {
                            MessageBox.Show($"Не удалось обновить роль в SQL Server: {sqlEx.Message}", 
                                "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }

                        MessageBox.Show("Сотрудник успешно обновлен!", "Редактирование", 
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    // Добавление нового сотрудника
                    await using var context = new PharmacyDbContext();
                    var newEmployee = new Employee
                    {
                        LastName = LastName,
                        FirstName = FirstName,
                        Patronymic = Patronymic,
                        Position = Position,
                        Department = Department,
                        HireDate = HireDate,
                        Phone = Phone,
                        Email = Email,
                        PassportSeries = PassportSeries,
                        PassportNumber = PassportNumber,
                        Salary = Salary,
                        IsManager = IsManager,
                        CanSignDocuments = CanSignDocuments,
                        IsActive = IsActive,
                        CreatedDate = DateTime.Now,
                        ModifiedDate = DateTime.Now
                    };

                    context.Employees.Add(newEmployee);
                    await context.SaveChangesAsync();

                    // Автоматически создаем учетную запись SQL Server
                    try
                    {
                        var password = "12345678";
                        await _userService.CreateUserAsync(newEmployee, password);
                        
                        var loginName = newEmployee.LastName.ToLower();
                        MessageBox.Show(
                            $"Сотрудник успешно добавлен!\n\n" +
                            $"Логин для входа: {loginName}\n" +
                            $"Пароль: {password}\n\n" +
                            $"Учетная запись SQL Server создана автоматически.",
                            "Добавление сотрудника", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception sqlEx)
                    {
                        MessageBox.Show(
                            $"Сотрудник добавлен, но не удалось создать учетную запись SQL Server.\n\n" +
                            $"Ошибка: {sqlEx.Message}\n\n" +
                            $"Убедитесь, что вы вошли под учетной записью с правами sysadmin.",
                            "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }

                RequestClose?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel()
        {
            RequestClose?.Invoke();
        }

        public void Dispose()
        {
            // Контекст больше не хранится как поле, поэтому ничего не нужно disposing
        }
    }
}
