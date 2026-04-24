using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using PharmacyClient.Data;
using PharmacyClient.Models;

namespace PharmacyClient.ViewModels
{
    public partial class ComponentsViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<Component> _components = new();

        [ObservableProperty]
        private Component? _selectedComponent;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private ObservableCollection<string> _categories = new();

        [ObservableProperty]
        private string _filterCategory = "Все";

        [ObservableProperty]
        private ObservableCollection<UnitsOfMeasure> _unitsOfMeasure = new();

        public ComponentsViewModel()
        {
            Categories.Add("Все");
        }

        [RelayCommand]
        private async Task LoadComponentsAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Загрузка данных...";

                await using var context = new PharmacyDbContext();
                var query = context.Components
                    .Include(c => c.Category)
                    .Include(c => c.Unit)
                    .AsNoTracking()
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    query = query.Where(c =>
                        c.ComponentName.Contains(SearchText) ||
                        (c.Supplier != null && c.Supplier.Contains(SearchText)));
                }

                if (!string.IsNullOrEmpty(FilterCategory) && FilterCategory != "Все")
                {
                    query = query.Where(c => c.Category != null && c.Category.CategoryName == FilterCategory);
                }

                var components = await query.OrderBy(c => c.ComponentName).ToListAsync();

                Components.Clear();
                foreach (var comp in components)
                {
                    Components.Add(comp);
                }

                await LoadCategoriesAsync();
                await LoadUnitsAsync();

                StatusMessage = $"Загружено компонентов: {Components.Count}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка загрузки: {ex.Message}";
                MessageBox.Show($"Ошибка загрузки компонентов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadCategoriesAsync()
        {
            try
            {
                await using var context = new PharmacyDbContext();
                var categories = await context.MedicineCategories
                    .Where(c => c.CategoryName != null)
                    .Select(c => c.CategoryName!)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();

                Categories.Clear();
                Categories.Add("Все");
                foreach (var cat in categories)
                {
                    Categories.Add(cat);
                }
            }
            catch
            {
                // Игнорируем ошибки
            }
        }

        private async Task LoadUnitsAsync()
        {
            try
            {
                await using var context = new PharmacyDbContext();
                UnitsOfMeasure = new ObservableCollection<UnitsOfMeasure>(
                    await context.UnitsOfMeasures.AsNoTracking().ToListAsync());
            }
            catch
            {
                // Игнорируем ошибки
            }
        }

        [RelayCommand]
        private void Search()
        {
            LoadComponentsCommand.Execute(null);
        }

        [RelayCommand]
        private void ClearSearch()
        {
            SearchText = string.Empty;
            FilterCategory = "Все";
            LoadComponentsCommand.Execute(null);
        }

        partial void OnFilterCategoryChanged(string value)
        {
            LoadComponentsCommand.Execute(null);
        }

        [RelayCommand]
        private async Task RefreshAsync()
        {
            await LoadComponentsAsync();
        }
    }
}
