using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using PharmacyClient.Data;
using PharmacyClient.Models;
using PharmacyClient.ViewModels;

namespace PharmacyClient.Views
{
    public partial class ComponentsView : UserControl
    {
        private readonly ComponentsViewModel _viewModel;
        private Component? _editingComponent;

        public ComponentsView()
        {
            InitializeComponent();
            _viewModel = new ComponentsViewModel();
            DataContext = _viewModel;
            
            // Загружаем данные при инициализации
            _viewModel.LoadComponentsCommand.Execute(null);
        }

        private void DataGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            if (e.Row.Item is Component component)
            {
                _editingComponent = component;
            }
        }

        private async void DataGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if (e.Row.Item is Component editedComponent && editedComponent.ComponentId > 0)
            {
                try
                {
                    await using var context = new PharmacyDbContext();
                    var componentToUpdate = await context.Components.FindAsync(editedComponent.ComponentId);
                    
                    if (componentToUpdate != null)
                    {
                        componentToUpdate.ComponentName = editedComponent.ComponentName;
                        componentToUpdate.Supplier = editedComponent.Supplier;
                        componentToUpdate.CriticalNorm = editedComponent.CriticalNorm;
                        componentToUpdate.CurrentStock = editedComponent.CurrentStock;
                        componentToUpdate.PurchasePrice = editedComponent.PurchasePrice;
                        componentToUpdate.StorageConditions = editedComponent.StorageConditions;
                        componentToUpdate.ModifiedDate = DateTime.Now;

                        await context.SaveChangesAsync();
                        
                        _viewModel.StatusMessage = "Компонент обновлен";
                        await _viewModel.LoadComponentsAsync();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    e.Cancel = true;
                }
            }
        }
    }
}
