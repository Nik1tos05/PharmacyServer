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
        private async Task RefreshAsync()
        {
            await LoadDoctorsAsync();
        }
    }
}
