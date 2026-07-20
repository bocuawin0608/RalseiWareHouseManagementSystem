using System.Windows;
using Microsoft.Extensions.Configuration;

namespace RalseiWarehouse;

public partial class App : Application
{
    public static string ConnectionString { get; private set; } = string.Empty;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .Build();

        ConnectionString = config.GetConnectionString("RalseiWarehouse")
            ?? throw new InvalidOperationException("Connection string 'RalseiWarehouse' not found in appsettings.json.");

        var login = new LoginWindow();
        login.ShowDialog();

        if (login.LoginSucceeded && login.AuthenticatedUser is not null)
        {
            var main = new MainWindow(login.AuthenticatedUser);
            main.Show();
        }
        else
        {
            Shutdown();
        }
    }
}
