using System.Windows.Controls;
using PharmacyClient.ViewModels;

namespace PharmacyClient.Views
{
    public partial class DoctorsView : UserControl
    {
        private readonly DoctorsViewModel _viewModel;

        public DoctorsView()
        {
            InitializeComponent();
            _viewModel = new DoctorsViewModel();
            DataContext = _viewModel;
            
            // Загружаем данные при инициализации
            _viewModel.LoadDoctorsCommand.Execute(null);
        }
    }
}
