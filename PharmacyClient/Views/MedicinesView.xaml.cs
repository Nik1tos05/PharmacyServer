using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using PharmacyClient.Data;
using PharmacyClient.Models;
using PharmacyClient.ViewModels;

namespace PharmacyClient.Views
{
    public partial class MedicinesView : UserControl
    {
        private readonly MedicinesViewModel _viewModel;
        private Medicine? _editingMedicine;

        public MedicinesView()
        {
            InitializeComponent();
            _viewModel = new MedicinesViewModel();
            DataContext = _viewModel;
            
            // Загружаем данные при инициализации
            _viewModel.LoadMedicinesCommand.Execute(null);
        }

        private void DataGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            if (e.Row.Item is Medicine medicine)
            {
                _editingMedicine = medicine;
            }
        }

        private async void DataGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if (e.Row.Item is Medicine editedMedicine && editedMedicine.MedicineId > 0)
            {
                // Валидация: название не должно быть пустым
                if (string.IsNullOrWhiteSpace(editedMedicine.MedicineName))
                {
                    MessageBox.Show("Название лекарства не может быть пустым!", "Ошибка валидации", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    e.Cancel = true;
                    return;
                }

                try
                {
                    await using var context = new PharmacyDbContext();
                    var medicineToUpdate = await context.Medicines.FindAsync(editedMedicine.MedicineId);
                    
                    if (medicineToUpdate != null)
                    {
                        medicineToUpdate.MedicineName = editedMedicine.MedicineName.Trim();
                        medicineToUpdate.Description = editedMedicine.Description;
                        medicineToUpdate.CriticalNorm = editedMedicine.CriticalNorm;
                        medicineToUpdate.CurrentStock = editedMedicine.CurrentStock;
                        medicineToUpdate.ManufacturingCost = editedMedicine.ManufacturingCost;
                        medicineToUpdate.SalePrice = editedMedicine.SalePrice;
                        medicineToUpdate.RequiresPrescription = editedMedicine.RequiresPrescription;
                        medicineToUpdate.IsReadyMade = editedMedicine.IsReadyMade;
                        medicineToUpdate.ModifiedDate = DateTime.Now;

                        await context.SaveChangesAsync();
                        
                        _viewModel.StatusMessage = "Лекарство обновлено";
                        await _viewModel.LoadMedicinesAsync();
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
