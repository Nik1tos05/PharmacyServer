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

        private void DataGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            // Разрешаем редактирование
        }

        private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            // Обработка окончания редактирования ячейки
        }

        private void DataGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            // Сохранение изменений при окончании редактирования строки
            if (e.EditAction == DataGridEditAction.Commit)
            {
                _viewModel.SaveEditedUserCommand.Execute(e.Row.Item);
            }
        }
    }
}
