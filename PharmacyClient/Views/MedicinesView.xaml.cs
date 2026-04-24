using System.Windows.Controls;
using PharmacyClient.ViewModels;

namespace PharmacyClient.Views
{
    public partial class MedicinesView : UserControl
    {
        private readonly MedicinesViewModel _viewModel;

        public MedicinesView()
        {
            InitializeComponent();
            _viewModel = new MedicinesViewModel();
            DataContext = _viewModel;
            
            // Загружаем данные при инициализации
            _viewModel.LoadMedicinesCommand.Execute(null);
        }
    }
}
