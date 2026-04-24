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
                        (o.Medicine != null && o.Medicine.Name.Contains(SearchText)));
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
            MessageBox.Show("Функция добавления заказа будет реализована через форму создания заказа по рецепту.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        [RelayCommand(CanExecute = nameof(HasSelectedOrder))]
        private void EditOrder()
        {
            if (SelectedOrder == null) return;
            MessageBox.Show($"Редактирование заказа №{SelectedOrder.OrderNumber}", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
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
                MessageBox.Show("Функция удаления будет реализована с проверкой прав доступа.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private bool HasSelectedOrder() => SelectedOrder != null;
    }
}
