using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using PharmacyClient.Data;
using PharmacyClient.Models;
using PharmacyClient.ViewModels;

namespace PharmacyClient.Views
{
    public partial class DoctorsView : UserControl
    {
        private readonly DoctorsViewModel _viewModel;
        private Doctor? _editingDoctor;

        public DoctorsView()
        {
            InitializeComponent();
            _viewModel = new DoctorsViewModel();
            DataContext = _viewModel;
            
            // Загружаем данные при инициализации
            _viewModel.LoadDoctorsCommand.Execute(null);
        }

        private void DataGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            if (e.Row.Item is Doctor doctor)
            {
                _editingDoctor = doctor;
            }
        }

        private async void DataGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if (e.Row.Item is Doctor editedDoctor && editedDoctor.DoctorId > 0)
            {
                try
                {
                    await using var context = new PharmacyDbContext();
                    var doctorToUpdate = await context.Doctors.FindAsync(editedDoctor.DoctorId);
                    
                    if (doctorToUpdate != null)
                    {
                        doctorToUpdate.LastName = editedDoctor.LastName;
                        doctorToUpdate.FirstName = editedDoctor.FirstName;
                        doctorToUpdate.Patronymic = editedDoctor.Patronymic;
                        doctorToUpdate.Specialization = editedDoctor.Specialization;
                        doctorToUpdate.LicenseNumber = editedDoctor.LicenseNumber;
                        doctorToUpdate.ClinicName = editedDoctor.ClinicName;
                        doctorToUpdate.Phone = editedDoctor.Phone;
                        doctorToUpdate.Email = editedDoctor.Email;
                        doctorToUpdate.IsActive = editedDoctor.IsActive;
                        doctorToUpdate.ModifiedDate = DateTime.Now;

                        await context.SaveChangesAsync();
                        
                        _viewModel.StatusMessage = "Врач обновлен";
                        await _viewModel.LoadDoctorsAsync();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    e.Cancel = true;
                }
            }
        }
    }
}
