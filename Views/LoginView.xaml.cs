using System.Windows;
using System.Windows.Input;
using RalseiWarehouse_v2.Controllers;
using RalseiWarehouse_v2.Session;

namespace RalseiWarehouse_v2.Views;

public partial class LoginView : Window
{
    private readonly LoginController _controller = new();

    // Set when login succeeds; App.xaml.cs reads it to build MainWindow.
    public UserSession? Session { get; private set; }

    public LoginView()
    {
        InitializeComponent();
        txtUserName.Focus();
    }

    private void btnLogin_Click(object sender, RoutedEventArgs e) => TryLogin();

    private void txtPassword_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            TryLogin();
    }

    private void btnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void TryLogin()
    {
        var session = _controller.Login(txtUserName.Text.Trim(), txtPassword.Password);
        if (session == null)
        {
            MessageBox.Show("Invalid username or password.", "Login failed",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            txtPassword.Clear();
            txtPassword.Focus();
            return;
        }

        Session = session;
        DialogResult = true;
        Close();
    }
}
