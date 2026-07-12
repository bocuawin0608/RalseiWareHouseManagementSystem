using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RalseiWarehouse.Models;
using RalseiWarehouse.Services;
using UserAccount = RalseiWarehouse.Models.User;

namespace RalseiWarehouse.ViewModels;

/// <summary>Coordinates the login use case.</summary>
public partial class LoginViewModel(IWarehouseService service) : ViewModelBase
{
    [ObservableProperty] private string userName = string.Empty; [ObservableProperty] private string password = string.Empty;
    /// <summary>Raised when valid credentials are accepted.</summary>
    public event Action<UserAccount>? Succeeded;
    /// <summary>Authenticates the entered credentials.</summary>
    [RelayCommand] private async Task LoginAsync() => await ExecuteAsync(async () => { if (string.IsNullOrWhiteSpace(UserName) || string.IsNullOrEmpty(Password)) throw new InvalidOperationException("Enter username and password."); var user = await service.LoginAsync(UserName.Trim(), Password); if (user is null) throw new InvalidOperationException("Invalid username or password."); Succeeded?.Invoke(user); });
}
