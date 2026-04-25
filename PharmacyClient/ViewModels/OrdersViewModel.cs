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

        [ObservableProperty]
        private ObservableCollection<Medicine> _medicinesList = new();

        public OrdersViewModel()
        {
            _context = new PharmacyDbContext();
        }

        [RelayCommand]
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
        private void AddOrder()
        {
            try
            {
                // Создаем новый заказ с данными по умолчанию
                var newOrder = new Order
                {
                    OrderNumber = $"ORD-{DateTime.Now:yyyyMMddHHmmss}",
                    OrderDate = DateTime.Now,
                    PatientId = 1, // Нужно будет выбрать пациента
                    MedicineId = MedicinesList.FirstOrDefault()?.MedicineId ?? 0,
                    Quantity = 1,
                    TotalPrice = 0,
                    OrderStatus = "Новый",
                    CreatedDate = DateTime.Now,
                    ModifiedDate = DateTime.Now
                };

                _context.Orders.Add(newOrder);
                _context.SaveChanges();

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
        private async Task DeleteOrderAsync()
        {
            if (SelectedOrder == null) return;

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить заказ №{SelectedOrder.OrderNumber}?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _context.Orders.Remove(SelectedOrder);
                    await _context.SaveChangesAsync();

                    Orders.Remove(SelectedOrder);
                    TotalCount = Orders.Count;
                    SelectedOrder = null;
                    StatusMessage = "Заказ успешно удален";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Ошибка удаления заказа: {ex.Message}";
                    MessageBox.Show($"Ошибка удаления заказа: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private bool HasSelectedOrder() => SelectedOrder != null;
    }
}
