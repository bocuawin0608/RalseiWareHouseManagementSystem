using System.Windows;
using System.Windows.Controls;
using RalseiWarehouse.ViewModels;

namespace RalseiWarehouse.Views;

/// <summary>Hosts credential controls without database or business logic.</summary>
public partial class LoginWindow : Window
{
    /// <summary>Initializes the login view.</summary>
    public LoginWindow() => InitializeComponent();
    /// <summary>Copies PasswordBox input to the view model because Password is not bindable.</summary>
    /// <param name="sender">The password box.</param><param name="e">Event data.</param>
    private void PasswordChanged(object sender, RoutedEventArgs e) { if (DataContext is LoginViewModel vm && sender is PasswordBox box) vm.Password = box.Password; }
}
