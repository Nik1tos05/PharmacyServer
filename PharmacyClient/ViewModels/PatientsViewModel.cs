using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using PharmacyClient.Data;
using PharmacyClient.Models;

namespace PharmacyClient.ViewModels
{
    public partial class PatientsViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<Patient> _patients = new();

        [ObservableProperty]
        private Patient? _selectedPatient;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        public PatientsViewModel()
        {
        }

        [RelayCommand]
        public async Task LoadPatientsAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Загрузка данных...";

                await using var context = new PharmacyDbContext();
                var query = context.Patients.AsNoTracking().AsQueryable();

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    query = query.Where(p =>
                        p.LastName.Contains(SearchText) ||
                        p.FirstName.Contains(SearchText) ||
                        (p.Patronymic != null && p.Patronymic.Contains(SearchText)) ||
                        (p.Phone != null && p.Phone.Contains(SearchText)));
                }

                var patients = await query.OrderBy(p => p.LastName).ThenBy(p => p.FirstName).ToListAsync();

                Patients.Clear();
                foreach (var patient in patients)
                {
                    Patients.Add(patient);
                }

                StatusMessage = $"Загружено пациентов: {Patients.Count}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка загрузки: {ex.Message}";
                MessageBox.Show($"Ошибка загрузки пациентов: {ex.Message}", "Ошибка",
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
            LoadPatientsCommand.Execute(null);
        }

        [RelayCommand]
        private void ClearSearch()
        {
            SearchText = string.Empty;
            LoadPatientsCommand.Execute(null);
        }

        [RelayCommand]
        private async Task AddPatientAsync()
        {
            try
            {
                await using var context = new PharmacyDbContext();
                var newPatient = new Patient
                {
                    LastName = "Новый",
                    FirstName = "Пациент",
                    RegistrationDate = DateTime.Now,
                    CreatedDate = DateTime.Now,
                    ModifiedDate = DateTime.Now
                };

                context.Patients.Add(newPatient);
                await context.SaveChangesAsync();

                StatusMessage = "Пациент добавлен";
                await LoadPatientsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка добавления: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task EditPatientAsync()
        {
            if (SelectedPatient == null)
            {
                MessageBox.Show("Выберите пациента для редактирования", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Редактирование через inline в DataGrid
            MessageBox.Show("Для редактирования нажмите на ячейку таблицы и измените значение", "Информация",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        [RelayCommand]
        private async Task DeletePatientAsync()
        {
            if (SelectedPatient == null)
            {
                MessageBox.Show("Выберите пациента для удаления", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var fullName = $"{SelectedPatient.LastName} {SelectedPatient.FirstName} {(string.IsNullOrEmpty(SelectedPatient.Patronymic) ? "" : SelectedPatient.Patronymic)}";
            var result = MessageBox.Show(
                $"Вы действительно хотите удалить пациента {fullName}?",
                "Удаление пациента", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                await using var context = new PharmacyDbContext();
                var patientToDelete = await context.Patients.FindAsync(SelectedPatient.PatientId);
                if (patientToDelete != null)
                {
                    context.Patients.Remove(patientToDelete);
                    await context.SaveChangesAsync();
                }

                StatusMessage = "Пациент удален";
                SelectedPatient = null;
                await LoadPatientsAsync();
                
                MessageBox.Show("Пациент успешно удален", "Удаление пациента", 
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
            await LoadPatientsAsync();
        }
    }
}
