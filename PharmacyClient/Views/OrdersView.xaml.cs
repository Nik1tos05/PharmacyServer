using System.Windows.Controls;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using PharmacyClient.Data;
using PharmacyClient.Models;
using System.Linq;

namespace PharmacyClient.Views
{
    public partial class OrdersView : UserControl
    {
        public OrdersView()
        {
            InitializeComponent();
        }

        private async void OrdersGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            // Метод больше не нужен, так как колонка "Препарат" удалена
        }

        private async void OrdersGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            // Сохраняем изменения при завершении редактирования строки
            if (e.Row.Item is Order editedOrder && DataContext is ViewModels.OrdersViewModel viewModel)
            {
                try
                {
                    await using var context = new PharmacyDbContext();
                    
                    // Обновляем заказ в базе данных
                    var existingOrder = await context.Orders.FindAsync(editedOrder.OrderId);
                    if (existingOrder != null)
                    {
                        existingOrder.Quantity = editedOrder.Quantity;
                        existingOrder.OrderStatus = editedOrder.OrderStatus;
                        existingOrder.ReadyDate = editedOrder.ReadyDate;
                        existingOrder.PickupDate = editedOrder.PickupDate;
                        
                        existingOrder.ModifiedDate = DateTime.Now;
                        
                        await context.SaveChangesAsync();
                        viewModel.StatusMessage = "Заказ обновлен";
                        await viewModel.LoadOrdersCommand.ExecuteAsync(null);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    e.Cancel = true; // Отменяем закрытие режима редактирования при ошибке
                }
            }
        }
    }
}
