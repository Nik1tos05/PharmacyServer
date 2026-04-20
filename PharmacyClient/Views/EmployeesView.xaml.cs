using System.Windows.Controls;
using PharmacyClient.ViewModels;

namespace PharmacyClient.Views
{
    public partial class EmployeesView : UserControl
    {
        private readonly EmployeesViewModel _viewModel;

        public EmployeesView()
        {
            InitializeComponent();
            _viewModel = new EmployeesViewModel();
            DataContext = _viewModel;
            
            // Загружаем данные при инициализации
            _viewModel.LoadEmployeesCommand.Execute(null);
        }
    }
}
