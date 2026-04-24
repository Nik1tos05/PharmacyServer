using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using PharmacyClient.Data;
using PharmacyClient.Models;
using PharmacyClient.ViewModels;

namespace PharmacyClient.Views
{
    public partial class PatientsView : UserControl
    {
        private readonly PatientsViewModel _viewModel;
        private Patient? _editingPatient;

        public PatientsView()
        {
            InitializeComponent();
            _viewModel = new PatientsViewModel();
            DataContext = _viewModel;
            
            // Загружаем данные при инициализации
            _viewModel.LoadPatientsCommand.Execute(null);
        }

        private void DataGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            if (e.Row.Item is Patient patient)
            {
                _editingPatient = patient;
            }
        }

        private async void DataGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if (e.Row.Item is Patient editedPatient && editedPatient.PatientId > 0)
            {
                try
                {
                    await using var context = new PharmacyDbContext();
                    var patientToUpdate = await context.Patients.FindAsync(editedPatient.PatientId);
                    
                    if (patientToUpdate != null)
                    {
                        patientToUpdate.LastName = editedPatient.LastName;
                        patientToUpdate.FirstName = editedPatient.FirstName;
                        patientToUpdate.Patronymic = editedPatient.Patronymic;
                        patientToUpdate.BirthDate = editedPatient.BirthDate;
                        patientToUpdate.Phone = editedPatient.Phone;
                        patientToUpdate.Email = editedPatient.Email;
                        patientToUpdate.Address = editedPatient.Address;
                        patientToUpdate.ModifiedDate = DateTime.Now;

                        await context.SaveChangesAsync();
                        
                        _viewModel.StatusMessage = "Пациент обновлен";
                        await _viewModel.LoadPatientsAsync();
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
