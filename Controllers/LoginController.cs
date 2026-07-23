using RalseiWarehouse_v2.Services;
using RalseiWarehouse_v2.Session;

namespace RalseiWarehouse_v2.Controllers;

// The one workflow that runs before MainWindow exists.
public class LoginController
{
    private readonly IAuthService _authService = new AuthService();

    public UserSession? Login(string username, string password)
    {
        var account = _authService.Login(username, password);
        return account == null ? null : new UserSession(account);
    }
}
