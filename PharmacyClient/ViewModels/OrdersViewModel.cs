using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using PharmacyClient.Data;
using PharmacyClient.Models;
using System.Linq;
using System.Threading.Tasks;

namespace PharmacyClient.ViewModels
{
    public partial class OrdersViewModel : ObservableObject
    {
        private readonly PharmacyDbContext _context;

        [ObservableProperty]
        private ObservableCollection<Order> _orders = new();

        [ObservableProperty]
        private Order? _selectedOrder;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private string _statusFilter = "Все";

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _statusMessage = "Готов к работе";

        [ObservableProperty]
        private int _totalCount;

        [ObservableProperty]
        private ObservableCollection<string> _statusOptions = new() { "Все", "Новый", "В производстве", "Готов", "Выдан", "Отменен" };

        public OrdersViewModel()
        {
            _context = new PharmacyDbContext();
            LoadOrdersCommand = new AsyncRelayCommand(LoadOrdersAsync);
        }

        public IAsyncRelayCommand LoadOrdersCommand { get; }

        private async Task LoadOrdersAsync()
        {
            IsLoading = true;
            StatusMessage = "Загрузка заказов...";

            try
            {
                var query = _context.Orders
                    .Include(o => o.Patient)
                    .Include(o => o.Medicine)
                    .Include(o => o.Prescription)
                    .Include(o => o.AssignedEmployee)
                    .Include(o => o.ProductionEmployee)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(StatusFilter) && StatusFilter != "Все")
                {
                    query = query.Where(o => o.OrderStatus == StatusFilter);
                }

                if (!string.IsNullOrEmpty(SearchText))
                {
                    query = query.Where(o =>
                        o.OrderNumber.Contains(SearchText) ||
                        (o.Patient != null && (o.Patient.LastName.Contains(SearchText) || o.Patient.FirstName.Contains(SearchText))) ||
                        (o.Medicine != null && o.Medicine.MedicineName.Contains(SearchText)));
                }

                var ordersList = await query.OrderByDescending(o => o.OrderDate).ToListAsync();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Orders.Clear();
                    foreach (var order in ordersList)
                    {
                        Orders.Add(order);
                    }
                    TotalCount = Orders.Count;
                    StatusMessage = $"Загружено {TotalCount} заказов";
                });
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка загрузки: {ex.Message}";
                MessageBox.Show($"Ошибка загрузки заказов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task AddOrderAsync()
        {
            try
            {
                // Создаем новый заказ с данными по умолчанию
                var newOrder = new Order
                {
                    OrderNumber = $"ORD-{DateTime.Now:yyyyMMddHHmmss}",
                    OrderDate = DateTime.Now,
                    PatientId = 1, // Нужно будет выбрать пациента
                    MedicineId = 1,
                    Quantity = 1,
                    TotalPrice = 0,
                    OrderStatus = "Новый",
                    CreatedDate = DateTime.Now,
                    ModifiedDate = DateTime.Now
                };

                await using var context2 = new PharmacyDbContext();
                context2.Orders.Add(newOrder);
                await context2.SaveChangesAsync();

                Orders.Insert(0, newOrder);
                TotalCount = Orders.Count;
                StatusMessage = "Заказ успешно добавлен. Нажмите на строку для редактирования.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка добавления заказа: {ex.Message}";
                MessageBox.Show($"Ошибка добавления заказа: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand(CanExecute = nameof(HasSelectedOrder))]
        private void DeleteOrder()
        {
            if (SelectedOrder == null) return;

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить заказ №{SelectedOrder.OrderNumber}?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        await using var context = new PharmacyDbContext();
                        var orderToDelete = await context.Orders.FindAsync(SelectedOrder.OrderId);
                        if (orderToDelete != null)
                        {
                            context.Orders.Remove(orderToDelete);
                            await context.SaveChangesAsync();

                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                Orders.Remove(SelectedOrder);
                                TotalCount = Orders.Count;
                                SelectedOrder = null;
                                StatusMessage = "Заказ успешно удален";
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            StatusMessage = $"Ошибка удаления заказа: {ex.Message}";
                            MessageBox.Show($"Ошибка удаления заказа: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        });
                    }
                });
            }
        }

        private bool HasSelectedOrder() => SelectedOrder != null;
    }
}
