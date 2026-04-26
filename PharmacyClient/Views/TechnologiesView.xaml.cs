using System.Windows.Controls;
using PharmacyClient.ViewModels;

namespace PharmacyClient.Views
{
    public partial class TechnologiesView : UserControl
    {
        private readonly TechnologiesViewModel _viewModel;

        public TechnologiesView()
        {
            InitializeComponent();
            _viewModel = new TechnologiesViewModel();
            DataContext = _viewModel;
            
            // Загружаем данные при инициализации
            _viewModel.LoadTechnologiesCommand.Execute(null);
        }
    }
}
