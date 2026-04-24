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
    public partial class WarehouseViewModel : ObservableObject
    {
        private readonly PharmacyDbContext _context;

        [ObservableProperty]
        private ObservableCollection<StockMovement> _stockMovements = new();

        [ObservableProperty]
        private ObservableCollection<InventoryCheck> _inventoryChecks = new();

        [ObservableProperty]
        private StockMovement? _selectedMovement;

        [ObservableProperty]
        private InventoryCheck? _selectedInventory;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private string _movementTypeFilter = "Все";

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _statusMessage = "Готов к работе";

        [ObservableProperty]
        private int _movementsCount;

        [ObservableProperty]
        private int _inventoriesCount;

        [ObservableProperty]
        private int _activeTab;

        [ObservableProperty]
        private ObservableCollection<string> _movementTypeOptions = new() { "Все", "Приход", "Расход", "Списание", "Перемещение", "Инвентаризация" };

        public WarehouseViewModel()
        {
            _context = new PharmacyDbContext();
        }

        [RelayCommand]
        private async Task LoadMovementsAsync()
        {
            IsLoading = true;
            StatusMessage = "Загрузка движений товаров...";

            try
            {
                var query = _context.StockMovements
                    .Include(sm => sm.Unit)
                    .Include(sm => sm.PerformedByEmployee)
                    .Include(sm => sm.RelatedOrder)
                    .Include(sm => sm.RelatedInventory)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(MovementTypeFilter) && MovementTypeFilter != "Все")
                {
                    query = query.Where(sm => sm.MovementType == MovementTypeFilter);
                }

                if (!string.IsNullOrEmpty(SearchText))
                {
                    query = query.Where(sm =>
                        sm.DocumentNumber.Contains(SearchText) ||
                        (sm.Reason != null && sm.Reason.Contains(SearchText)));
                }

                var movementsList = await query.OrderByDescending(sm => sm.MovementDate).ToListAsync();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    StockMovements.Clear();
                    foreach (var movement in movementsList)
                    {
                        StockMovements.Add(movement);
                    }
                    MovementsCount = StockMovements.Count;
                    StatusMessage = $"Загружено {MovementsCount} записей о движениях";
                });
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка загрузки: {ex.Message}";
                MessageBox.Show($"Ошибка загрузки движений: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task LoadInventoriesAsync()
        {
            IsLoading = true;
            StatusMessage = "Загрузка инвентаризаций...";

            try
            {
                var query = _context.InventoryChecks
                    .Include(ic => ic.ConductedByEmployee)
                    .Include(ic => ic.CheckedByEmployee)
                    .AsQueryable();

                var inventoriesList = await query.OrderByDescending(ic => ic.CheckDate).ToListAsync();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    InventoryChecks.Clear();
                    foreach (var inventory in inventoriesList)
                    {
                        InventoryChecks.Add(inventory);
                    }
                    InventoriesCount = InventoryChecks.Count;
                    StatusMessage = $"Загружено {InventoriesCount} инвентаризаций";
                });
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка загрузки: {ex.Message}";
                MessageBox.Show($"Ошибка загрузки инвентаризаций: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task RefreshAllAsync()
        {
            await LoadMovementsAsync();
            await LoadInventoriesAsync();
        }

        [RelayCommand]
        private void AddMovement()
        {
            MessageBox.Show("Функция добавления движения товара будет реализована через форму проведения операции.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        [RelayCommand]
        private void AddInventory()
        {
            MessageBox.Show("Функция создания новой инвентаризации будет реализована с формой проведения инвентаризации.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
