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
    public partial class TechnologiesViewModel : ObservableObject
    {
        private readonly PharmacyDbContext _context;

        [ObservableProperty]
        private ObservableCollection<PreparationTechnology> _technologies = new();

        [ObservableProperty]
        private PreparationTechnology? _selectedTechnology;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private string _medicineTypeFilter = "Все";

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _statusMessage = "Готов к работе";

        [ObservableProperty]
        private int _totalCount;

        [ObservableProperty]
        private ObservableCollection<string> _medicineTypeOptions = new() { "Все" };

        [ObservableProperty]
        private ObservableCollection<MedicineType> _allMedicineTypes = new();

        public TechnologiesViewModel()
        {
            _context = new PharmacyDbContext();
        }

        [RelayCommand]
        private async Task LoadTechnologiesAsync()
        {
            IsLoading = true;
            StatusMessage = "Загрузка технологий...";

            try
            {
                // Загружаем типы лекарств для фильтра
                var types = await _context.MedicineTypes.ToListAsync();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    AllMedicineTypes.Clear();
                    MedicineTypeOptions.Clear();
                    MedicineTypeOptions.Add("Все");
                    foreach (var type in types)
                    {
                        AllMedicineTypes.Add(type);
                        MedicineTypeOptions.Add(type.TypeName);
                    }
                });

                var query = _context.PreparationTechnologies
                    .Include(t => t.MedicineType)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(MedicineTypeFilter) && MedicineTypeFilter != "Все")
                {
                    query = query.Where(t => t.MedicineType.TypeName == MedicineTypeFilter);
                }

                if (!string.IsNullOrEmpty(SearchText))
                {
                    query = query.Where(t =>
                        t.TechnologyCode.Contains(SearchText) ||
                        t.MedicineName.Contains(SearchText) ||
                        t.PreparationMethod.Contains(SearchText));
                }

                var technologiesList = await query.OrderByDescending(t => t.CreatedDate).ToListAsync();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Technologies.Clear();
                    foreach (var tech in technologiesList)
                    {
                        Technologies.Add(tech);
                    }
                    TotalCount = Technologies.Count;
                    StatusMessage = $"Загружено {TotalCount} технологий";
                });
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка загрузки: {ex.Message}";
                MessageBox.Show($"Ошибка загрузки технологий: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void AddTechnology()
        {
            MessageBox.Show("Функция добавления технологии будет реализована через форму редактирования.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        [RelayCommand(CanExecute = nameof(HasSelectedTechnology))]
        private void EditTechnology()
        {
            if (SelectedTechnology == null) return;
            MessageBox.Show($"Редактирование технологии: {SelectedTechnology.MedicineName}", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        [RelayCommand(CanExecute = nameof(HasSelectedTechnology))]
        private void DeleteTechnology()
        {
            if (SelectedTechnology == null) return;

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить технологию '{SelectedTechnology.MedicineName}'?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                MessageBox.Show("Функция удаления будет реализована с проверкой прав доступа.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private bool HasSelectedTechnology() => SelectedTechnology != null;
    }
}
