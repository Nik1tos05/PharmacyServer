using System.Windows;

namespace PharmacyClient
{
    public partial class App : Application
    {
        private static UserSession? _currentUserSession;

        public static UserSession? CurrentUserSession
        {
            get => _currentUserSession;
            set
            {
                _currentUserSession = value;
                if (value == null)
                {
                    // Logout - navigate to login
                    Current?.Dispatcher.Invoke(() =>
                    {
                        var loginWindow = new Views.LoginView();
                        loginWindow.Show();
                        Current?.MainWindow?.Close();
                        Current!.MainWindow = loginWindow;
                    });
                }
            }
        }

        public static void SetCurrentUserSession(UserSession session)
        {
            _currentUserSession = session;
        }
    }

    public class UserSession
    {
        public int EmployeeId { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string? Patronymic { get; set; }
        public string Position { get; set; } = string.Empty;
        public string? Department { get; set; }
        public bool IsManager { get; set; }
        public bool CanSignDocuments { get; set; }
        public string ConnectionString { get; set; } = string.Empty;
        
        // Computed role based on employee properties
        public UserRole Role
        {
            get
            {
                if (IsManager && CanSignDocuments)
                    return UserRole.Administrator;
                if (CanSignDocuments)
                    return UserRole.Manager;
                if (Position.Contains("Фармацевт", StringComparison.OrdinalIgnoreCase))
                    return UserRole.Pharmacist;
                if (Position.Contains("Лаборант", StringComparison.OrdinalIgnoreCase))
                    return UserRole.LabAssistant;
                return UserRole.Employee;
            }
        }

        public string FullName => $"{LastName} {FirstName} {(string.IsNullOrEmpty(Patronymic) ? "" : Patronymic)}".Trim();
    }

    public enum UserRole
    {
        Employee,           // Basic access
        Pharmacist,         // Access to medicines, prescriptions
        LabAssistant,       // Access to components, preparation technologies
        Manager,            // Can sign documents, approve requests
        Administrator       // Full access to all tables
    }
}
