using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RalseiWarehouse.Data;
using RalseiWarehouse.Services;
using RalseiWarehouse.ViewModels;
using RalseiWarehouse.Views;

namespace RalseiWarehouse;

/// <summary>Configures dependency injection and application lifetime.</summary>
public partial class App : Application
{
    private ServiceProvider? provider;
    /// <summary>Builds services and displays the login screen.</summary>
    /// <param name="e">Startup event data.</param>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e); var configuration = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory).AddJsonFile("appsettings.json").Build();
        var connection = configuration.GetConnectionString("RalseiWarehouse") ?? throw new InvalidOperationException("The database connection string is missing.");
        var services = new ServiceCollection(); services.AddDbContextFactory<WarehouseDbContext>(options => options.UseSqlServer(connection)); services.AddSingleton<IWarehouseService, WarehouseService>(); services.AddTransient<LoginViewModel>(); services.AddTransient<MasterDataViewModel>(); services.AddTransient<TransactionViewModel>(); services.AddTransient<InventoryViewModel>(); services.AddTransient<AdministrationViewModel>(); provider = services.BuildServiceProvider(); ShowLogin();
    }
    /// <summary>Disposes application services.</summary>
    /// <param name="e">Exit event data.</param>
    protected override void OnExit(ExitEventArgs e) { provider?.Dispose(); base.OnExit(e); }
    /// <summary>Creates login and transitions to the authenticated shell.</summary>
    private void ShowLogin()
    {
        var services = provider ?? throw new InvalidOperationException("Services are unavailable."); var vm = services.GetRequiredService<LoginViewModel>(); var login = new LoginWindow { DataContext = vm }; MainWindow = login;
        vm.Succeeded += user => { var shell = new MainViewModel(user, services.GetRequiredService<MasterDataViewModel>(), services.GetRequiredService<TransactionViewModel>(), services.GetRequiredService<InventoryViewModel>(), services.GetRequiredService<AdministrationViewModel>()); var window = new MainWindow { DataContext = shell }; MainWindow = window; window.Show(); login.Close(); if(shell.IsManager){_=shell.MasterData.LoadAsync();_=shell.Inventory.LoadAsync();} _=shell.Transactions.LoadAsync(); if(shell.IsAdministrator)_=shell.Administration.LoadAsync(); }; login.Show();
    }
}
