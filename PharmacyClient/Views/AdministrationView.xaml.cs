using System.Windows.Controls;
using PharmacyClient.ViewModels;

namespace PharmacyClient.Views
{
    public partial class AdministrationView : UserControl
    {
        private readonly AdministrationViewModel _viewModel;

        public AdministrationView()
        {
            InitializeComponent();
            _viewModel = new AdministrationViewModel();
            DataContext = _viewModel;
            
            // Загружаем данные при инициализации
            _viewModel.LoadUserAccountsCommand.Execute(null);
        }
    }
}
