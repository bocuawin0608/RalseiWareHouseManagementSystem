using System.Windows;
using Microsoft.EntityFrameworkCore;
using RalseiWarehouse.Data;
using RalseiWarehouse.Models;

namespace RalseiWarehouse;

public partial class LoginWindow : Window
{
    public bool LoginSucceeded { get; private set; }
    public User? AuthenticatedUser { get; private set; }

    public LoginWindow()
    {
        InitializeComponent();
    }

    private async void BtnLogin_Click(object sender, RoutedEventArgs e)
    {
        var username = txtUsername.Text.Trim();
        var password = txtPassword.Password;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
        {
            txtMessage.Text = "Enter username and password.";
            return;
        }

        try
        {
            await using var db = new WarehouseDbContext(App.ConnectionString);
            var user = await db.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserName == username);

            if (user is null || user.Password != password)
            {
                txtMessage.Text = "Invalid username or password.";
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
