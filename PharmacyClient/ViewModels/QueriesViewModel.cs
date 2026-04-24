using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using PharmacyClient.Data;
using PharmacyClient.Models;
using PharmacyClient.Services;

namespace PharmacyClient.ViewModels
{
    /// <summary>
    /// ViewModel для выполнения LINQ-запросов к базе данных аптеки
    /// </summary>
    public class QueriesViewModel : INotifyPropertyChanged
    {
        private readonly PharmacyDbContext _context;
        private readonly PharmacyQueriesService _queriesService;

        private QueryInfo? _selectedQuery;
        private bool _isLoading;
        private string _statusMessage = string.Empty;
        private int _resultCount;
        private bool _showParameters;
        private bool _showCategoryParameter;
        private bool _showDateRangeParameter;
        private bool _showMedicineParameter;
        private bool _showMedicineTypeParameter;
        private bool _medicineSelectionRequired;
        private bool _showMedicineListForQuery;

        private MedicineCategory? _selectedCategory;
        private DateTime? _startDate;
        private DateTime? _endDate;
        private Medicine? _selectedMedicine;
        private MedicineType? _selectedMedicineType;

        public QueriesViewModel()
        {
            _context = new PharmacyDbContext();
            _queriesService = new PharmacyQueriesService(_context);

            AvailableQueries = new ObservableCollection<QueryInfo>
            {
                new QueryInfo { Id = 1, DisplayName = "1. Покупатели, не забравшие заказ вовремя", ParameterType = QueryParameterType.None },
                new QueryInfo { Id = 2, DisplayName = "2. Покупатели, ждущие прибытия медикаментов", ParameterType = QueryParameterType.Category },
                new QueryInfo { Id = 3, DisplayName = "3. Топ-10 наиболее используемых медикаментов", ParameterType = QueryParameterType.Category },
                new QueryInfo { Id = 4, DisplayName = "4. Объем использованных веществ за период", ParameterType = QueryParameterType.DateRange },
                new QueryInfo { Id = 5, DisplayName = "5. Покупатели, заказывавшие лекарства", ParameterType = QueryParameterType.DateRange | QueryParameterType.Medicine | QueryParameterType.MedicineType },
                new QueryInfo { Id = 6, DisplayName = "6. Лекарства с критической нормой или закончившиеся", ParameterType = QueryParameterType.None },
                new QueryInfo { Id = 7, DisplayName = "7. Лекарства с минимальным запасом", ParameterType = QueryParameterType.Category },
                new QueryInfo { Id = 8, DisplayName = "8. Заказы в производстве", ParameterType = QueryParameterType.None },
                new QueryInfo { Id = 9, DisplayName = "9. Препараты для заказов в производстве", ParameterType = QueryParameterType.None },
                new QueryInfo { Id = 10, DisplayName = "10. Технологии приготовления лекарств", ParameterType = QueryParameterType.MedicineType },
                new QueryInfo { Id = 11, DisplayName = "11. Цены на лекарство и компоненты", ParameterType = QueryParameterType.Medicine },
                new QueryInfo { Id = 12, DisplayName = "12. Наиболее активные покупатели", ParameterType = QueryParameterType.MedicineType | QueryParameterType.Medicine },
                new QueryInfo { Id = 13, DisplayName = "13. Сведения о конкретном лекарстве", ParameterType = QueryParameterType.Medicine },
                new QueryInfo { Id = 14, DisplayName = "Доп: Просроченные лекарства", ParameterType = QueryParameterType.None },
                new QueryInfo { Id = 15, DisplayName = "Доп: Недостача по инвентаризации", ParameterType = QueryParameterType.None },
                new QueryInfo { Id = 16, DisplayName = "Доп: Информация по рецепту", ParameterType = QueryParameterType.None }
            };

            Categories = new ObservableCollection<MedicineCategory>();
            Medicines = new ObservableCollection<Medicine>();
            MedicineTypes = new ObservableCollection<MedicineType>();

            LoadReferenceDataCommand = new RelayCommand(LoadReferenceData);
            ExecuteQueryCommand = new RelayCommand(ExecuteQuery, () => SelectedQuery != null);
            ApplyParametersCommand = new RelayCommand(ApplyParameters);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        // Properties
        public ObservableCollection<QueryInfo> AvailableQueries { get; }

        public QueryInfo? SelectedQuery
        {
            get => _selectedQuery;
            set
            {
                if (SetField(ref _selectedQuery, value))
                {
                    UpdateParameterVisibility();
                    ((RelayCommand)ExecuteQueryCommand).NotifyCanExecuteChanged();
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetField(ref _isLoading, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetField(ref _statusMessage, value);
        }

        public int ResultCount
        {
            get => _resultCount;
            set => SetField(ref _resultCount, value);
        }

        public bool ShowParameters
        {
            get => _showParameters;
            set => SetField(ref _showParameters, value);
        }

        public bool ShowCategoryParameter
        {
            get => _showCategoryParameter;
            set => SetField(ref _showCategoryParameter, value);
        }

        public bool ShowDateRangeParameter
        {
            get => _showDateRangeParameter;
            set => SetField(ref _showDateRangeParameter, value);
        }

        public bool ShowMedicineParameter
        {
            get => _showMedicineParameter;
            set => SetField(ref _showMedicineParameter, value);
        }

        public bool ShowMedicineTypeParameter
        {
            get => _showMedicineTypeParameter;
            set => SetField(ref _showMedicineTypeParameter, value);
        }

        public bool MedicineSelectionRequired
        {
            get => _medicineSelectionRequired;
            set => SetField(ref _medicineSelectionRequired, value);
        }

        public bool ShowMedicineListForQuery
        {
            get => _showMedicineListForQuery;
            set => SetField(ref _showMedicineListForQuery, value);
        }

        public ObservableCollection<MedicineCategory> Categories { get; }

        public MedicineCategory? SelectedCategory
        {
            get => _selectedCategory;
            set => SetField(ref _selectedCategory, value);
        }

        public ObservableCollection<Medicine> Medicines { get; }

        public Medicine? SelectedMedicine
        {
            get => _selectedMedicine;
            set 
            { 
                if (SetField(ref _selectedMedicine, value))
                {
                    // Если лекарство выбрано в DataGrid для запросов 11 и 13, скрываем список и выполняем запрос
                    if (ShowMedicineListForQuery && value != null)
                    {
                        ShowMedicineListForQuery = false;
                        ExecuteQuery();
                    }
                }
            }
        }

        public ObservableCollection<MedicineType> MedicineTypes { get; }

        public MedicineType? SelectedMedicineType
        {
            get => _selectedMedicineType;
            set => SetField(ref _selectedMedicineType, value);
        }

        public DateTime? StartDate
        {
            get => _startDate;
            set => SetField(ref _startDate, value);
        }

        public DateTime? EndDate
        {
            get => _endDate;
            set => SetField(ref _endDate, value);
        }

        // Commands
        public ICommand LoadReferenceDataCommand { get; }
        public ICommand ExecuteQueryCommand { get; }
        public ICommand ApplyParametersCommand { get; }

        private void LoadReferenceData()
        {
            try
            {
                var categories = _context.MedicineCategories.ToList();
                Categories.Clear();
                foreach (var cat in categories)
                    Categories.Add(cat);

                var medicines = _context.Medicines.Include(m => m.Category).Include(m => m.MedicineType).ToList();
                Medicines.Clear();
                foreach (var med in medicines)
                    Medicines.Add(med);

                var types = _context.MedicineTypes.ToList();
                MedicineTypes.Clear();
                foreach (var type in types)
                    MedicineTypes.Add(type);

                StatusMessage = $"Загружено справочных данных: категории ({categories.Count}), лекарства ({medicines.Count}), типы ({types.Count})";
                
                // Уведомляем интерфейс об обновлении коллекций для запросов 11 и 13
                OnPropertyChanged(nameof(Medicines));
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка загрузки справочников: {ex.Message}";
            }
        }

        private void UpdateParameterVisibility()
        {
            if (SelectedQuery == null)
            {
                ShowParameters = false;
                MedicineSelectionRequired = false;
                ShowMedicineListForQuery = false;
                return;
            }

            ShowParameters = SelectedQuery.ParameterType != QueryParameterType.None;
            ShowCategoryParameter = (SelectedQuery.ParameterType & QueryParameterType.Category) == QueryParameterType.Category;
            ShowDateRangeParameter = (SelectedQuery.ParameterType & QueryParameterType.DateRange) == QueryParameterType.DateRange;
            ShowMedicineParameter = (SelectedQuery.ParameterType & QueryParameterType.Medicine) == QueryParameterType.Medicine;
            ShowMedicineTypeParameter = (SelectedQuery.ParameterType & QueryParameterType.MedicineType) == QueryParameterType.MedicineType;

            // Для запросов 11 и 13 требуется выбор лекарства - показываем реестр лекарств в окне результатов
            MedicineSelectionRequired = SelectedQuery.Id == 11 || SelectedQuery.Id == 13;
            
            // Для запросов 11 и 13 показываем список лекарств для выбора в окне результатов
            ShowMedicineListForQuery = MedicineSelectionRequired;
            
            // Для запросов 11 и 13 скрываем обычный параметр лекарства, так как показываем расширенный список
            if (MedicineSelectionRequired)
            {
                ShowMedicineParameter = false;
            }

            // Устанавливаем значения по умолчанию для дат
            if (ShowDateRangeParameter && !StartDate.HasValue)
            {
                StartDate = DateTime.Now.AddMonths(-1);
                EndDate = DateTime.Now;
            }
        }

        private void ApplyParameters()
        {
            ExecuteQuery();
        }

        private async void ExecuteQuery()
        {
            if (SelectedQuery == null) return;

            try
            {
                IsLoading = true;
                StatusMessage = "Выполнение запроса...";

                object? result = SelectedQuery.Id switch
                {
                    1 => ExecuteQuery1(),
                    2 => ExecuteQuery2(),
                    3 => ExecuteQuery3(),
                    4 => ExecuteQuery4(),
                    5 => ExecuteQuery5(),
                    6 => ExecuteQuery6(),
                    7 => ExecuteQuery7(),
                    8 => ExecuteQuery8(),
                    9 => ExecuteQuery9(),
                    10 => ExecuteQuery10(),
                    11 => ExecuteQuery11(),
                    12 => ExecuteQuery12(),
                    13 => ExecuteQuery13(),
                    14 => ExecuteQuery14(),
                    15 => ExecuteQuery15(),
                    16 => ExecuteQuery16(),
                    _ => null
                };

                if (result != null)
                {
                    QueryResults = result;
                    ResultCount = GetResultCount(result);
                    StatusMessage = $"Запрос {SelectedQuery.DisplayName} выполнен успешно";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка выполнения запроса: {ex.Message}";
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка выполнения запроса", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private object? ExecuteQuery1()
        {
            var result = _queriesService.GetPatientsWhoDidNotPickupOrders();
            return result.Patients;
        }

        private object? ExecuteQuery2()
        {
            var categoryId = SelectedCategory?.CategoryId;
            var result = _queriesService.GetPatientsWaitingForMedicines(categoryId);
            return result.Patients;
        }

        private object? ExecuteQuery3()
        {
            var categoryId = SelectedCategory?.CategoryId;
            return _queriesService.GetTop10MostUsedMedicines(categoryId);
        }

        private object? ExecuteQuery4()
        {
            if (!StartDate.HasValue || !EndDate.HasValue)
            {
                MessageBox.Show("Укажите период времени", "Предупреждение", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }
            
            try
            {
                return _queriesService.GetComponentUsage(StartDate.Value, EndDate.Value);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка выполнения запроса 4: {ex.Message}";
                MessageBox.Show($"Ошибка запроса 4: {ex.Message}\n\nДетали: {ex.InnerException?.Message ?? "Нет дополнительных деталей"}", 
                    "Ошибка выполнения запроса 4", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        private object? ExecuteQuery5()
        {
            if (!StartDate.HasValue || !EndDate.HasValue)
            {
                MessageBox.Show("Укажите период времени", "Предупреждение", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            var medicineId = SelectedMedicine?.MedicineId;
            List<int>? typeIds = SelectedMedicineType != null ? new List<int> { SelectedMedicineType.MedicineTypeId } : null;

            var result = _queriesService.GetPatientsWhoOrderedMedicines(
                StartDate.Value, EndDate.Value, medicineId, typeIds);
            return result.Patients;
        }

        private object? ExecuteQuery6()
        {
            return _queriesService.GetMedicinesAtCriticalNormOrOutOfStock();
        }

        private object? ExecuteQuery7()
        {
            var categoryId = SelectedCategory?.CategoryId;
            return _queriesService.GetMedicinesWithMinimumStock(categoryId);
        }

        private object? ExecuteQuery8()
        {
            var result = _queriesService.GetOrdersInProduction();
            return result.Orders;
        }

        private object? ExecuteQuery9()
        {
            var result = _queriesService.GetRequiredComponentsForProductionOrders();
            return result.Components;
        }

        private object? ExecuteQuery10()
        {
            int? typeId = SelectedMedicineType?.MedicineTypeId;
            return _queriesService.GetPreparationTechnologies(typeId.HasValue ? new List<int> { typeId.Value } : null);
        }

        private object? ExecuteQuery11()
        {
            // Если лекарство не выбрано, показываем список лекарств в окне результатов для выбора
            if (SelectedMedicine == null)
            {
                // Загружаем все лекарства, если еще не загружены
                if (Medicines.Count == 0)
                {
                    LoadReferenceData();
                }
                
                // Показываем список лекарств в окне результатов
                QueryResults = Medicines.ToList();
                StatusMessage = "Выберите лекарство из списка выше и нажмите 'Выполнить' снова";
                return null;
            }
            
            try
            {
                var result = _queriesService.GetMedicinePriceInfo(SelectedMedicine.MedicineId);
                // Оборачиваем результат в коллекцию для корректного отображения в DataGrid
                return new[] { result };
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка выполнения запроса 11: {ex.Message}";
                MessageBox.Show($"Ошибка запроса 11: {ex.Message}", 
                    "Ошибка выполнения запроса 11", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        private object? ExecuteQuery12()
        {
            int? medicineId = SelectedMedicine?.MedicineId;
            int? typeId = SelectedMedicineType?.MedicineTypeId;
            return _queriesService.GetMostActiveCustomers(typeId, medicineId);
        }

        private object? ExecuteQuery13()
        {
            // Если лекарство не выбрано, показываем список лекарств в окне результатов для выбора
            if (SelectedMedicine == null)
            {
                // Загружаем все лекарства, если еще не загружены
                if (Medicines.Count == 0)
                {
                    LoadReferenceData();
                }
                
                // Показываем список лекарств в окне результатов
                QueryResults = Medicines.ToList();
                StatusMessage = "Выберите лекарство из списка выше и нажмите 'Выполнить' снова";
                return null;
            }
            
            try
            {
                return new[] { _queriesService.GetDetailedMedicineInfo(SelectedMedicine.MedicineId) };
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка выполнения запроса 13: {ex.Message}";
                MessageBox.Show($"Ошибка запроса 13: {ex.Message}", 
                    "Ошибка выполнения запроса 13", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        private object? ExecuteQuery14()
        {
            return _queriesService.GetExpiredMedicines();
        }

        private object? ExecuteQuery15()
        {
            return _queriesService.GetShortagesFromInventory();
        }

        private object? ExecuteQuery16()
        {
            // Для этого запроса нужен ID рецепта - упростим, взяв первый непустой
            var prescription = _context.Prescriptions.FirstOrDefault();
            if (prescription == null)
            {
                MessageBox.Show("Нет рецептов в базе данных", "Информация", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return null;
            }
            return new[] { _queriesService.GetPrescriptionInfo(prescription.PrescriptionId) };
        }

        private int GetResultCount(object result)
        {
            if (result is System.Collections.IEnumerable enumerable)
            {
                int count = 0;
                foreach (var _ in enumerable) count++;
                return count;
            }
            return 0;
        }

        // Результаты запроса (будут отображены в DataGrid)
        private object? _queryResults;
        public object? QueryResults
        {
            get => _queryResults;
            set => SetField(ref _queryResults, value);
        }
    }

    /// <summary>
    /// Информация о доступных запросах
    /// </summary>
    public class QueryInfo
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public QueryParameterType ParameterType { get; set; }
    }

    [Flags]
    public enum QueryParameterType
    {
        None = 0,
        Category = 1,
        DateRange = 2,
        Medicine = 4,
        MedicineType = 8
    }

    /// <summary>
    /// Простая реализация RelayCommand
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action? _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object? parameter) => _execute?.Invoke();

        public void NotifyCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
