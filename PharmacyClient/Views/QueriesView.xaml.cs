using System.Windows.Controls;
using PharmacyClient.ViewModels;

namespace PharmacyClient.Views
{
    public partial class QueriesView : UserControl
    {
        private readonly QueriesViewModel _viewModel;

        public QueriesView()
        {
            InitializeComponent();
            _viewModel = new QueriesViewModel();
            DataContext = _viewModel;

            // Загружаем справочные данные при инициализации
            _viewModel.LoadReferenceDataCommand.Execute(null);
        }
    }
}
