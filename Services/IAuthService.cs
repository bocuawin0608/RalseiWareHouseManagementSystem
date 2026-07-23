using RalseiWarehouse_v2.Models;

namespace RalseiWarehouse_v2.Services;

public interface IAuthService
{
    // Returns the account when credentials are valid, null otherwise.
    Account? Login(string username, string password);
}
