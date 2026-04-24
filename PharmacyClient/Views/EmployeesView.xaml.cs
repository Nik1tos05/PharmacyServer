using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using PharmacyClient.Data;
using PharmacyClient.Models;
using PharmacyClient.ViewModels;

namespace PharmacyClient.Views
{
    public partial class EmployeesView : UserControl
    {
        private readonly EmployeesViewModel _viewModel;
        private Employee? _editingEmployee;

        public EmployeesView()
        {
            InitializeComponent();
            _viewModel = new EmployeesViewModel();
            DataContext = _viewModel;
            
            // Загружаем данные при инициализации
            _viewModel.LoadEmployeesCommand.Execute(null);
        }

        private void DataGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            // Сохраняем ссылку на редактируемую запись
            if (e.Row.Item is Employee employee)
            {
                _editingEmployee = employee;
            }
        }

        private async void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            // Обработка окончания редактирования ячейки
        }

        private async void DataGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if (e.Row.Item is Employee editedEmployee && editedEmployee.EmployeeId > 0)
            {
                try
                {
                    await using var context = new PharmacyDbContext();
                    var employeeToUpdate = await context.Employees.FindAsync(editedEmployee.EmployeeId);
                    
                    if (employeeToUpdate != null)
                    {
                        // Обновляем только изменяемые поля
                        employeeToUpdate.LastName = editedEmployee.LastName;
                        employeeToUpdate.FirstName = editedEmployee.FirstName;
                        employeeToUpdate.Patronymic = editedEmployee.Patronymic;
                        employeeToUpdate.Position = editedEmployee.Position;
                        employeeToUpdate.Department = editedEmployee.Department;
                        employeeToUpdate.Phone = editedEmployee.Phone;
                        employeeToUpdate.Email = editedEmployee.Email;
                        employeeToUpdate.IsManager = editedEmployee.IsManager;
                        employeeToUpdate.CanSignDocuments = editedEmployee.CanSignDocuments;
                        employeeToUpdate.IsActive = editedEmployee.IsActive;
                        employeeToUpdate.ModifiedDate = DateTime.Now;

                        await context.SaveChangesAsync();
                        
                        _viewModel.StatusMessage = "Сотрудник обновлен";
                        
                        // Перезагружаем данные для отображения актуальных значений
                        await _viewModel.LoadEmployeesAsync();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    // Отменяем изменения при ошибке
                    e.Cancel = true;
                }
            }
        }
    }
}
