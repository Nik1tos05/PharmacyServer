using System.Windows.Controls;
using PharmacyClient.ViewModels;

namespace PharmacyClient.Views
{
    public partial class PatientsView : UserControl
    {
        private readonly PatientsViewModel _viewModel;

        public PatientsView()
        {
            InitializeComponent();
            _viewModel = new PatientsViewModel();
            DataContext = _viewModel;
            
            // Загружаем данные при инициализации
            _viewModel.LoadPatientsCommand.Execute(null);
        }
    }
}
