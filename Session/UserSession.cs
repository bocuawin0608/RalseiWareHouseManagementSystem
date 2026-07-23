using RalseiWarehouse_v2.Models;

namespace RalseiWarehouse_v2.Session;

// The authenticated identity, produced by LoginController and threaded
// into every controller/service call that stamps PerformedBy / AssignedTo.
public class UserSession
{
    public int AccountId { get; }
    public string DisplayName { get; }
    public string RoleName { get; }

    public UserSession(Account account)
    {
        AccountId = account.AccountId;
        DisplayName = account.DisplayName;
        RoleName = account.Role?.Name ?? string.Empty;
    }
}
