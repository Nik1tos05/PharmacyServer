using System.Windows;
using PharmacyClient.ViewModels;

namespace PharmacyClient.Views
{
    public partial class LoginView : Window
    {
        public LoginView()
        {
            InitializeComponent();
            
            // Load employees on startup
            if (DataContext is LoginViewModel vm)
            {
                vm.LoadEmployeesCommand.Execute(null);
            }
        }
    }
}
