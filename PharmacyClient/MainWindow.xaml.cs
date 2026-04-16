using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using PharmacyClient.Data;

namespace PharmacyClient
{
    public partial class MainWindow : Window
    {
        private readonly PharmacyDbContext _context;

        public MainWindow()
        {
            InitializeComponent();
            _context = new PharmacyDbContext();
            
            // Set up the main window based on user role
            SetupRoleBasedAccess();
        }

        private void SetupRoleBasedAccess()
        {
            var session = App.CurrentUserSession;
            if (session == null)
            {
                MessageBox.Show("Ошибка: пользователь не авторизован", "Ошибка", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }

            // Update UI with user info
            Title = $"Аптечная Система - {session.FullName} ({session.Position})";
            UserInfoText.Text = $"{session.FullName}";
            UserNameText.Text = $"ФИО: {session.FullName}";
            UserPositionText.Text = $"Должность: {session.Position}";
            UserDepartmentText.Text = $"Отдел: {session.Department ?? "Не указан"}";
            UserRoleText.Text = $"Роль: {GetRoleName(session.Role)}";
            UserPermissionsText.Text = $"Права: {GetPermissionsDescription(session.Role)}";
            
            // Populate access rights panel
            PopulateAccessRightsPanel(session.Role);
            
            // Apply role-based visibility to tabs and menu items
            ApplyRoleBasedVisibility(session.Role);
            
            // Test database connection
            TestDatabaseConnection();
        }

        private string GetRoleName(UserRole role)
        {
            return role switch
            {
                UserRole.Employee => "Сотрудник",
                UserRole.Pharmacist => "Фармацевт",
                UserRole.LabAssistant => "Лаборант",
                UserRole.Manager => "Менеджер",
                UserRole.Administrator => "Администратор",
                _ => "Неизвестная роль"
            };
        }

        private string GetPermissionsDescription(UserRole role)
        {
            return role switch
            {
                UserRole.Employee => "Базовый доступ к справочникам",
                UserRole.Pharmacist => "Доступ к лекарствам, рецептам, пациентам",
                UserRole.LabAssistant => "Доступ к компонентам, технологиям приготовления",
                UserRole.Manager => "Доступ к складу, заказам, инвентаризации, подписи документов",
                UserRole.Administrator => "Полный доступ ко всем функциям системы",
                _ => "Нет прав доступа"
            };
        }

        private void PopulateAccessRightsPanel(UserRole role)
        {
            AccessRightsPanel.Children.Clear();
            
            var rights = new List<string>();
            
            // All roles have access to reference data
            rights.Add("✓ Справочники");
            
            if (role >= UserRole.Pharmacist)
            {
                rights.Add("✓ Лекарства");
                rights.Add("✓ Рецепты");
            }
            
            if (role >= UserRole.LabAssistant)
            {
                rights.Add("✓ Компоненты");
                rights.Add("✓ Технологии");
            }
            
            if (role >= UserRole.Manager)
            {
                rights.Add("✓ Склад");
                rights.Add("✓ Заказы");
                rights.Add("✓ Инвентаризация");
            }
            
            if (role >= UserRole.Administrator)
            {
                rights.Add("✓ Сотрудники");
                rights.Add("✓ Администрирование");
            }
            
            foreach (var right in rights)
            {
                var border = new Border
                {
                    BorderBrush = System.Windows.Media.Brushes.LightGray,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(5),
                    Padding = new Thickness(10, 5, 10, 5),
                    Margin = new Thickness(5),
                    Child = new TextBlock
                    {
                        Text = right,
                        FontWeight = FontWeights.Normal
                    }
                };
                AccessRightsPanel.Children.Add(border);
            }
        }

        private void ApplyRoleBasedVisibility(UserRole role)
        {
            // Tab visibility
            MedicinesTab.Visibility = role >= UserRole.Pharmacist ? Visibility.Visible : Visibility.Collapsed;
            ComponentsTab.Visibility = role >= UserRole.LabAssistant ? Visibility.Visible : Visibility.Collapsed;
            PrescriptionsTab.Visibility = role >= UserRole.Pharmacist ? Visibility.Visible : Visibility.Collapsed;
            WarehouseTab.Visibility = role >= UserRole.Manager ? Visibility.Visible : Visibility.Collapsed;
            OrdersTab.Visibility = role >= UserRole.Manager ? Visibility.Visible : Visibility.Collapsed;
            EmployeesTab.Visibility = role >= UserRole.Administrator ? Visibility.Visible : Visibility.Collapsed;
            AdminTab.Visibility = role >= UserRole.Administrator ? Visibility.Visible : Visibility.Collapsed;
            
            // Menu item visibility
            foreach (MenuItem menuItem in MainMenu.Items)
            {
                ApplyMenuVisibility(menuItem, role);
            }
        }

        private void ApplyMenuVisibility(MenuItem menuItem, UserRole role)
        {
            if (menuItem.Tag is string requiredRoles)
            {
                bool isVisible = false;
                
                if (requiredRoles == "All")
                {
                    isVisible = true;
                }
                else
                {
                    var roles = requiredRoles.Split(',');
                    foreach (var r in roles)
                    {
                        if (Enum.TryParse<UserRole>(r.Trim(), out var requiredRole))
                        {
                            if (role >= requiredRole)
                            {
                                isVisible = true;
                                break;
                            }
                        }
                    }
                }
                
                menuItem.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
            }
            
            // Recursively check submenus
            foreach (MenuItem subItem in menuItem.Items)
            {
                ApplyMenuVisibility(subItem, role);
            }
        }

        private async void TestDatabaseConnection()
        {
            try
            {
                var employeeCount = await _context.Employees.CountAsync();
                ConnectionStatusText.Text = $"Подключено к БД (сотрудников: {employeeCount})";
                StatusText.Text = "База данных подключена";
            }
            catch (Exception ex)
            {
                ConnectionStatusText.Text = "Ошибка подключения к БД";
                StatusText.Text = $"Ошибка: {ex.Message}";
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы действительно хотите выйти из системы?", 
                "Выход", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                App.CurrentUserSession = null;
                
                var loginWindow = new Views.LoginView();
                loginWindow.Show();
                Close();
            }
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
