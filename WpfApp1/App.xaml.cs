using System.Windows;
using Microsoft.Extensions.Configuration;
using RalseiWarehouse.Services;
using RalseiWarehouse.Views;

namespace RalseiWarehouse;

/// <summary>
/// Application entry point. Handles startup, database initialization, and the login/main window loop.
/// </summary>
public partial class App : Application
{
    /// <inheritdoc />
    protected override async void OnStartup(StartupEventArgs e)
    {
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        base.OnStartup(e);

        try
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();

            var connectionString = config.GetConnectionString("RalseiWarehouse")
                ?? throw new InvalidOperationException("Connection string 'RalseiWarehouse' not found in appsettings.json.");

            var service = WarehouseService.GetInstance(connectionString);
            await service.EnsureDatabaseAsync();

            while (true)
            {
                var login = new LoginWindow();
                login.ShowDialog();

                if (!login.LoginSucceeded || login.AuthenticatedUser is null)
                {
                    Shutdown();
                    return;
                }

                var main = new MainWindow(login.AuthenticatedUser);
                main.ShowDialog();

                if (!main.LoggedOut)
                {
                    Shutdown();
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }
}
