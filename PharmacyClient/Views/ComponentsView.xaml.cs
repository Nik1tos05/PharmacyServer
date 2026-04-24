using System.Windows.Controls;
using PharmacyClient.ViewModels;

namespace PharmacyClient.Views
{
    public partial class ComponentsView : UserControl
    {
        private readonly ComponentsViewModel _viewModel;

        public ComponentsView()
        {
            InitializeComponent();
            _viewModel = new ComponentsViewModel();
            DataContext = _viewModel;
            
            // Загружаем данные при инициализации
            _viewModel.LoadComponentsCommand.Execute(null);
        }
    }
}
