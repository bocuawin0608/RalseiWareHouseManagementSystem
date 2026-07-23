using System.Windows;
using RalseiWarehouse_v2.Session;
using RalseiWarehouse_v2.Views;

namespace RalseiWarehouse_v2;

// App shell: built only after login succeeds. Hosts the workflow views
// and threads the session into each of them.
public partial class MainWindow : Window
{
    private readonly UserSession _session;

    // App.xaml.cs checks this to decide whether to show the login again.
    public bool RestartLogin { get; private set; }

    public MainWindow(UserSession session)
    {
        _session = session;
        InitializeComponent();

        txtUser.Text = $"{session.DisplayName} ({session.RoleName})";
        ViewHost.Content = new WorkQueueView(_session);   // workers land on their queue
    }

    private void btnNavWorkQueue_Click(object sender, RoutedEventArgs e)
        => ViewHost.Content = new WorkQueueView(_session);

    private void btnNavInbound_Click(object sender, RoutedEventArgs e)
        => ViewHost.Content = new InboundOrderView(_session);

    private void btnNavOutbound_Click(object sender, RoutedEventArgs e)
        => ViewHost.Content = new OutboundOrderView(_session);

    private void btnNavExceptions_Click(object sender, RoutedEventArgs e)
        => ViewHost.Content = new ExceptionView(_session);

    private void btnLogout_Click(object sender, RoutedEventArgs e)
    {
        RestartLogin = true;
        Close();
    }
}
