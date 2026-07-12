using RalseiWarehouse.Models;
using UserAccount = RalseiWarehouse.Models.User;

namespace RalseiWarehouse.ViewModels;

/// <summary>Provides the authenticated application shell.</summary>
public sealed class MainViewModel
{
    /// <summary>Creates the shell and its feature view models.</summary>
    public MainViewModel(UserAccount user, MasterDataViewModel masterData, TransactionViewModel transactions, InventoryViewModel inventory, AdministrationViewModel administration) { User = user; MasterData = masterData; Transactions = transactions; Inventory = inventory; Administration = administration; var role = user.Role.DisplayName ?? ""; IsAdministrator = role.Contains("Admin", StringComparison.OrdinalIgnoreCase); IsManager = IsAdministrator || role.Contains("Manager", StringComparison.OrdinalIgnoreCase); }
    public UserAccount User { get; } public MasterDataViewModel MasterData { get; } public TransactionViewModel Transactions { get; } public InventoryViewModel Inventory { get; } public AdministrationViewModel Administration { get; } public bool IsAdministrator { get; } public bool IsManager { get; }
}
