using System.Windows;
using RalseiWarehouse_v2.Views;

namespace RalseiWarehouse_v2;

// Composition root: no StartupUri - we control the window flow manually.
// Login first; MainWindow only after a session exists. Logout loops back to login.
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        while (true)
        {
            var login = new LoginView();
            bool? loggedIn = login.ShowDialog();

            if (loggedIn != true || login.Session == null)
            {
                Shutdown();
                return;
            }

            var main = new MainWindow(login.Session);
            main.ShowDialog();

            if (!main.RestartLogin)
            {
                Shutdown();
                return;
            }
        }
    }
}
