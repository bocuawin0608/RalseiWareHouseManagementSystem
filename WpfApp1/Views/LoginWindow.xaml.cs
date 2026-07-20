using System.Windows;
using RalseiWarehouse.Services;

namespace RalseiWarehouse.Views;

/// <summary>
/// Login dialog for user authentication.
/// </summary>
public partial class LoginWindow : Window
{
    /// <summary>
    /// Gets a value indicating whether login was successful.
    /// </summary>
    public bool LoginSucceeded { get; private set; }

    /// <summary>
    /// Gets the authenticated user after a successful login.
    /// </summary>
    public Models.User? AuthenticatedUser { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LoginWindow"/> class.
    /// </summary>
    public LoginWindow()
    {
        InitializeComponent();
    }

    private async void BtnLogin_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var username = txtUsername.Text.Trim();
            var password = txtPassword.Password;

            var service = WarehouseService.GetInstance(string.Empty);
            var user = await service.AuthenticateAsync(username, password);

            if (user is null)
            {
                txtMessage.Text = string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password)
                    ? "Please enter username and password."
                    : "Invalid username or password.";
                return;
            }

            LoginSucceeded = true;
            AuthenticatedUser = user;
            Close();
        }
        catch (Exception ex)
        {
            txtMessage.Text = $"Error: {ex.Message}";
        }
    }
}
