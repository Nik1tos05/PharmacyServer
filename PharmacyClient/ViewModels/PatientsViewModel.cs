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
        private readonly PharmacyDbContext _context;

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
            _context = new PharmacyDbContext();
        }

        [RelayCommand]
        private async Task LoadPatientsAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Загрузка данных...";

                var query = _context.Patients.AsQueryable();

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
        private async Task RefreshAsync()
        {
            await LoadPatientsAsync();
        }
    }
}
