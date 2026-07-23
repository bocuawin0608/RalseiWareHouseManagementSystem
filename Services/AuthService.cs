using System.Linq;
using RalseiWarehouse_v2.Data;
using RalseiWarehouse_v2.Models;

namespace RalseiWarehouse_v2.Services;

public class AuthService : IAuthService
{
    public Account? Login(string username, string password)
    {
        using var db = new WarehouseDbContext();

        // PLAIN TEXT comparison for now (course requirement) - hash later.
        // Customers/suppliers have NULL UserName, so they can never match.
        var account = db.Accounts
            .Where(a => a.IsActive && a.UserName == username && a.PasswordHash == password)
            .FirstOrDefault();

        if (account == null)
            return null;

        // Attach role name for the session (two-step lookup, no lazy loading)
        account.Role = db.Roles.Find(account.RoleId);
        return account;
    }
}
