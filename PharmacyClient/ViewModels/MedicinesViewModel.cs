using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using PharmacyClient.Data;
using PharmacyClient.Models;

namespace PharmacyClient.ViewModels
{
    public partial class MedicinesViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<Medicine> _medicines = new();

        [ObservableProperty]
        private Medicine? _selectedMedicine;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private string _filterCategory = "Все";

        [ObservableProperty]
        private ObservableCollection<string> _categories = new();

        [ObservableProperty]
        private ObservableCollection<MedicineType> _medicineTypes = new();

        [ObservableProperty]
        private ObservableCollection<UnitsOfMeasure> _unitsOfMeasure = new();

        public MedicinesViewModel()
        {
            Categories.Add("Все");
        }

        [RelayCommand]
        public async Task LoadMedicinesAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Загрузка данных...";

                // Создаем новый контекст для каждой операции
                await using var context = new PharmacyDbContext();
                
                var query = context.Medicines
                    .Include(m => m.Category)
                    .Include(m => m.MedicineType)
                    .Include(m => m.Unit)
                    .AsNoTracking()
                    .AsQueryable();

                // Применяем поиск
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    query = query.Where(m => 
                        m.MedicineName.Contains(SearchText) ||
                        (m.Description != null && m.Description.Contains(SearchText)));
                }

                // Применяем фильтр по категории
                if (!string.IsNullOrEmpty(FilterCategory) && FilterCategory != "Все")
                {
                    query = query.Where(m => m.Category != null && m.Category.CategoryName == FilterCategory);
                }

                var medicines = await query.OrderBy(m => m.MedicineName).ToListAsync();

                Medicines.Clear();
                foreach (var med in medicines)
                {
                    Medicines.Add(med);
                }

                // Обновляем список категорий
                await LoadCategoriesAsync();
                
                // Загружаем типы лекарств и единицы измерения для редактора
                await LoadTypesAndUnitsAsync();

                StatusMessage = $"Загружено лекарств: {Medicines.Count}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка загрузки: {ex.Message}";
                MessageBox.Show($"Ошибка загрузки лекарств: {ex.Message}", "Ошибка", 
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
                // Игнорируем ошибки при загрузке категорий
            }
        }

        private async Task LoadTypesAndUnitsAsync()
        {
            try
            {
                await using var context = new PharmacyDbContext();
                MedicineTypes = new ObservableCollection<MedicineType>(
                    await context.MedicineTypes.AsNoTracking().ToListAsync());
                
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
            LoadMedicinesCommand.Execute(null);
        }

        [RelayCommand]
        private void ClearSearch()
        {
            SearchText = string.Empty;
            FilterCategory = "Все";
            LoadMedicinesCommand.Execute(null);
        }

        partial void OnFilterCategoryChanged(string value)
        {
            LoadMedicinesCommand.Execute(null);
        }

        [RelayCommand]
        private async Task AddMedicineAsync()
        {
            try
            {
                await using var context = new PharmacyDbContext();
                
                // Получаем первый доступный тип и единицу измерения
                var firstType = await context.MedicineTypes.FirstOrDefaultAsync();
                var firstUnit = await context.UnitsOfMeasures.FirstOrDefaultAsync();
                
                if (firstType == null || firstUnit == null)
                {
                    MessageBox.Show("Необходимо сначала создать типы лекарств и единицы измерения", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var newMedicine = new Medicine
                {
                    MedicineName = "Новое лекарство",
                    MedicineTypeId = firstType.MedicineTypeId,
                    UnitId = firstUnit.UnitId,
                    CriticalNorm = 10,
                    SalePrice = 0,
                    RequiresPrescription = false,
                    IsReadyMade = true,
                    CreatedDate = DateTime.Now,
                    ModifiedDate = DateTime.Now
                };

                context.Medicines.Add(newMedicine);
                await context.SaveChangesAsync();

                StatusMessage = "Лекарство добавлено";
                await LoadMedicinesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка добавления: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task EditMedicineAsync()
        {
            if (SelectedMedicine == null)
            {
                MessageBox.Show("Выберите лекарство для редактирования", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show("Для редактирования нажмите на ячейку таблицы и измените значение", "Информация",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        [RelayCommand]
        private async Task DeleteMedicineAsync()
        {
            if (SelectedMedicine == null)
            {
                MessageBox.Show("Выберите лекарство для удаления", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Вы действительно хотите удалить лекарство \"{SelectedMedicine.MedicineName}\"?",
                "Удаление лекарства", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                await using var context = new PharmacyDbContext();
                var medicineToDelete = await context.Medicines
                    .Include(m => m.MedicineCompositions)
                    .FirstOrDefaultAsync(m => m.MedicineId == SelectedMedicine.MedicineId);
                    
                if (medicineToDelete != null)
                {
                    // Сначала удаляем связанные записи состава
                    if (medicineToDelete.MedicineCompositions != null && medicineToDelete.MedicineCompositions.Any())
                    {
                        context.MedicineCompositions.RemoveRange(medicineToDelete.MedicineCompositions);
                    }
                    
                    context.Medicines.Remove(medicineToDelete);
                    await context.SaveChangesAsync();
                }

                StatusMessage = "Лекарство удалено";
                SelectedMedicine = null;
                await LoadMedicinesAsync();
                
                MessageBox.Show("Лекарство успешно удалено", "Удаление лекарства", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления: {ex.Message}\n\nВнутренняя ошибка: {ex.InnerException?.Message}", "Ошибка", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task RefreshAsync()
        {
            await LoadMedicinesAsync();
        }
    }
}
