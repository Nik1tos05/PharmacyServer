using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using PharmacyClient.Data;
using PharmacyClient.Models;

namespace PharmacyClient.ViewModels
{
    public partial class DoctorsViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<Doctor> _doctors = new();

        [ObservableProperty]
        private Doctor? _selectedDoctor;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        public DoctorsViewModel()
        {
        }

        [RelayCommand]
        private async Task LoadDoctorsAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Загрузка данных...";

                await using var context = new PharmacyDbContext();
                var query = context.Doctors.AsNoTracking().AsQueryable();

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    query = query.Where(d =>
                        d.LastName.Contains(SearchText) ||
                        d.FirstName.Contains(SearchText) ||
                        (d.Patronymic != null && d.Patronymic.Contains(SearchText)) ||
                        (d.Specialization != null && d.Specialization.Contains(SearchText)));
                }

                var doctors = await query.OrderBy(d => d.LastName).ThenBy(d => d.FirstName).ToListAsync();

                Doctors.Clear();
                foreach (var doctor in doctors)
                {
                    Doctors.Add(doctor);
                }

                StatusMessage = $"Загружено врачей: {Doctors.Count}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка загрузки: {ex.Message}";
                MessageBox.Show($"Ошибка загрузки врачей: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void Search()
        {
            LoadDoctorsCommand.Execute(null);
        }

        [RelayCommand]
        private void ClearSearch()
        {
            SearchText = string.Empty;
            LoadDoctorsCommand.Execute(null);
        }

        [RelayCommand]
        private async Task AddDoctorAsync()
        {
            try
            {
                await using var context = new PharmacyDbContext();
                var newDoctor = new Doctor
                {
                    LastName = "Новый",
                    FirstName = "Врач",
                    RegistrationDate = DateTime.Now,
                    CreatedDate = DateTime.Now,
                    ModifiedDate = DateTime.Now
                };

                context.Doctors.Add(newDoctor);
                await context.SaveChangesAsync();

                StatusMessage = "Врач добавлен";
                await LoadDoctorsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка добавления: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task EditDoctorAsync()
        {
            if (SelectedDoctor == null)
            {
                MessageBox.Show("Выберите врача для редактирования", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show("Для редактирования нажмите на ячейку таблицы и измените значение", "Информация",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        [RelayCommand]
        private async Task DeleteDoctorAsync()
        {
            if (SelectedDoctor == null)
            {
                MessageBox.Show("Выберите врача для удаления", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Вы действительно хотите удалить врача {SelectedDoctor.FullName}?",
                "Удаление врача", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                await using var context = new PharmacyDbContext();
                var doctorToDelete = await context.Doctors.FindAsync(SelectedDoctor.DoctorId);
                if (doctorToDelete != null)
                {
                    context.Doctors.Remove(doctorToDelete);
                    await context.SaveChangesAsync();
                }

                StatusMessage = "Врач удален";
                SelectedDoctor = null;
                await LoadDoctorsAsync();
                
                MessageBox.Show("Врач успешно удален", "Удаление врача", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task RefreshAsync()
        {
            await LoadDoctorsAsync();
        }
    }
}
