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
        private ObservableCollection<Employee> _employeesList = new();

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
            LoadEmployeesAsync().ConfigureAwait(false);
        }

        private async Task LoadEmployeesAsync()
        {
            try
            {
                var employees = await _context.Employees.OrderBy(e => e.LastName).ToListAsync();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    EmployeesList.Clear();
                    foreach (var emp in employees)
                    {
                        EmployeesList.Add(emp);
                    }
                });
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка загрузки сотрудников: {ex.Message}";
            }
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
            try
            {
                var newMovement = new StockMovement
                {
                    MovementDate = DateTime.Now,
                    DocumentNumber = $"DOC-{DateTime.Now:yyyyMMddHHmmss}",
                    MovementType = "Приход",
                    ItemType = "Medicine",
                    ItemId = 0,
                    Quantity = 1,
                    UnitId = EmployeesList.Count > 0 ? 1 : 0,
                    PreviousStock = 0,
                    NewStock = 1,
                    PerformedByEmployeeId = EmployeesList.FirstOrDefault()?.EmployeeId ?? 0,
                    Reason = "Новое движение",
                    CreatedDate = DateTime.Now
                };

                _context.StockMovements.Add(newMovement);
                _context.SaveChanges();

                StockMovements.Insert(0, newMovement);
                MovementsCount = StockMovements.Count;
                StatusMessage = "Движение товара успешно добавлено";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка добавления движения: {ex.Message}";
                MessageBox.Show($"Ошибка добавления движения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void AddInventory()
        {
            try
            {
                var newInventory = new InventoryCheck
                {
                    InventoryNumber = $"INV-{DateTime.Now:yyyyMMddHHmmss}",
                    CheckDate = DateTime.Now,
                    Status = "В процессе",
                    ConductedByEmployeeId = EmployeesList.FirstOrDefault()?.EmployeeId ?? 0,
                    TotalItemsChecked = 0,
                    DiscrepanciesFound = 0,
                    ExpiredItemsCount = 0,
                    CriticalNormViolations = 0,
                    ShortageValue = 0,
                    SurplusValue = 0,
                    ReportGenerated = false,
                    CreatedDate = DateTime.Now,
                    ModifiedDate = DateTime.Now
                };

                _context.InventoryChecks.Add(newInventory);
                _context.SaveChanges();

                InventoryChecks.Insert(0, newInventory);
                InventoriesCount = InventoryChecks.Count;
                StatusMessage = "Инвентаризация успешно создана";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка создания инвентаризации: {ex.Message}";
                MessageBox.Show($"Ошибка создания инвентаризации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand(CanExecute = nameof(HasSelectedMovement))]
        private async Task DeleteMovementAsync()
        {
            if (SelectedMovement == null) return;

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить движение №{SelectedMovement.DocumentNumber}?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _context.StockMovements.Remove(SelectedMovement);
                    await _context.SaveChangesAsync();

                    StockMovements.Remove(SelectedMovement);
                    MovementsCount = StockMovements.Count;
                    SelectedMovement = null;
                    StatusMessage = "Движение успешно удалено";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Ошибка удаления движения: {ex.Message}";
                    MessageBox.Show($"Ошибка удаления движения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand(CanExecute = nameof(HasSelectedInventory))]
        private async Task DeleteInventoryAsync()
        {
            if (SelectedInventory == null) return;

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить инвентаризацию №{SelectedInventory.InventoryNumber}?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _context.InventoryChecks.Remove(SelectedInventory);
                    await _context.SaveChangesAsync();

                    InventoryChecks.Remove(SelectedInventory);
                    InventoriesCount = InventoryChecks.Count;
                    SelectedInventory = null;
                    StatusMessage = "Инвентаризация успешно удалена";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Ошибка удаления инвентаризации: {ex.Message}";
                    MessageBox.Show($"Ошибка удаления инвентаризации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private bool HasSelectedMovement() => SelectedMovement != null;

        private bool HasSelectedInventory() => SelectedInventory != null;
    }
}
