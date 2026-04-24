using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PharmacyClient.ViewModels;

namespace PharmacyClient.Views
{
    public partial class WarehouseView : UserControl
    {
        public WarehouseView()
        {
            InitializeComponent();
        }

        private void StockMovementsGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            // Разрешаем редактирование
        }

        private async void StockMovementsGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            try
            {
                if (e.Row.DataContext is Models.StockMovement movement && e.EditingElement != null)
                {
                    var viewModel = DataContext as WarehouseViewModel;
                    if (viewModel == null) return;

                    // Сохраняем изменения в базе данных
                    using var context = new Data.PharmacyDbContext();
                    var existingMovement = await context.StockMovements.FindAsync(movement.MovementId);
                    
                    if (existingMovement != null)
                    {
                        // Обновляем только измененные поля
                        context.Entry(existingMovement).CurrentValues.SetValues(movement);
                        await context.SaveChangesAsync();
                        
                        viewModel.StatusMessage = "Движение товара обновлено";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения изменений: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InventoryChecksGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            // Разрешаем редактирование
        }

        private async void InventoryChecksGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            try
            {
                if (e.Row.DataContext is Models.InventoryCheck inventory && e.EditingElement != null)
                {
                    var viewModel = DataContext as WarehouseViewModel;
                    if (viewModel == null) return;

                    // Сохраняем изменения в базе данных
                    using var context = new Data.PharmacyDbContext();
                    var existingInventory = await context.InventoryChecks.FindAsync(inventory.InventoryId);
                    
                    if (existingInventory != null)
                    {
                        // Обновляем только измененные поля
                        context.Entry(existingInventory).CurrentValues.SetValues(inventory);
                        await context.SaveChangesAsync();
                        
                        viewModel.StatusMessage = "Инвентаризация обновлена";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения изменений: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
