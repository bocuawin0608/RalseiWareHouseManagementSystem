using Microsoft.EntityFrameworkCore;
using RalseiWarehouse.Data;
using RalseiWarehouse.Helpers;
using RalseiWarehouse.Models;
using Product = RalseiWarehouse.Models.Object;
using UserAccount = RalseiWarehouse.Models.User;

namespace RalseiWarehouse.Services;

/// <summary>A line requested for an import receipt.</summary>
public sealed record InputRequest(string ProductId, int Count, double InputPrice, double OutputPrice);
/// <summary>A line requested for an export receipt.</summary>
public sealed record OutputRequest(string ProductId, int Count);
/// <summary>A row in the derived inventory report.</summary>
public sealed record InventoryRow(string Id, string Product, string Unit, string Supplier, int Input, int Output, int Stock);

/// <summary>Defines the database operations consumed by view models.</summary>
public interface IWarehouseService
{
    /// <summary>Authenticates credentials.</summary>
    Task<UserAccount?> LoginAsync(string userName, string password);
    /// <summary>Loads units.</summary>
    Task<List<Unit>> GetUnitsAsync();
    /// <summary>Saves a unit.</summary>
    Task SaveUnitAsync(Unit item);
    /// <summary>Deletes a unit.</summary>
    Task DeleteUnitAsync(int id);
    /// <summary>Loads suppliers.</summary>
    Task<List<Supplier>> GetSuppliersAsync();
    /// <summary>Saves a supplier.</summary>
    Task SaveSupplierAsync(Supplier item);
    /// <summary>Deletes a supplier.</summary>
    Task DeleteSupplierAsync(int id);
    /// <summary>Loads customers.</summary>
    Task<List<Customer>> GetCustomersAsync();
    /// <summary>Saves a customer.</summary>
    Task SaveCustomerAsync(Customer item);
    /// <summary>Deletes a customer.</summary>
    Task DeleteCustomerAsync(int id);
    /// <summary>Loads products.</summary>
    Task<List<Product>> GetProductsAsync();
    /// <summary>Saves a product.</summary>
    Task SaveProductAsync(Product item);
    /// <summary>Deletes a product.</summary>
    Task DeleteProductAsync(string id);
    /// <summary>Loads roles.</summary>
    Task<List<Role>> GetRolesAsync();
    /// <summary>Saves a role.</summary>
    Task SaveRoleAsync(Role item);
    /// <summary>Deletes a role.</summary>
    Task DeleteRoleAsync(int id);
    /// <summary>Loads users.</summary>
    Task<List<UserAccount>> GetUsersAsync();
    /// <summary>Saves a user.</summary>
    Task SaveUserAsync(UserAccount item, string? password);
    /// <summary>Deletes a user.</summary>
    Task DeleteUserAsync(int id);
    /// <summary>Creates an import receipt.</summary>
    Task CreateInputAsync(IEnumerable<InputRequest> lines);
    /// <summary>Creates an export receipt.</summary>
    Task CreateOutputAsync(int customerId, IEnumerable<OutputRequest> lines);
    /// <summary>Loads derived inventory.</summary>
    Task<List<InventoryRow>> GetInventoryAsync(string? search);
}

/// <summary>Uses EF Core DbContext instances directly for warehouse use cases.</summary>
public sealed class WarehouseService(IDbContextFactory<WarehouseDbContext> factory) : IWarehouseService
{
    private UserAccount? currentUser;
    /// <summary>Authenticates a user and loads their role.</summary>
    public async Task<UserAccount?> LoginAsync(string userName, string password)
    {
        await using var db = await factory.CreateDbContextAsync();
        var user = await db.Users.Include(x => x.Role).SingleOrDefaultAsync(x => x.UserName == userName);
        if (user is null) return null;
        var usesHash = user.Password?.StartsWith("PBKDF2-SHA256$", StringComparison.Ordinal) == true;
        if (!(usesHash ? SecurityHelper.VerifyPassword(password, user.Password) : user.Password == password)) return null;
        if (!usesHash) { user.Password = SecurityHelper.HashPassword(password); await db.SaveChangesAsync(); }
        currentUser = user; return user;
    }

    /// <summary>Loads units.</summary>
    public async Task<List<Unit>> GetUnitsAsync() { await using var db = await factory.CreateDbContextAsync(); return await db.Units.AsNoTracking().OrderBy(x => x.DisplayName).ToListAsync(); }
    /// <summary>Saves a unit.</summary>
    public async Task SaveUnitAsync(Unit item) { EnsureManager(); ValidationHelper.Require(item.DisplayName, "Unit name"); await SaveAsync(item, item.UnitId == 0); }
    /// <summary>Deletes an unused unit.</summary>
    public Task DeleteUnitAsync(int id) { EnsureManager(); return DeleteAsync<Unit>(id, "The unit is used by a product."); }
    /// <summary>Loads suppliers.</summary>
    public async Task<List<Supplier>> GetSuppliersAsync() { await using var db = await factory.CreateDbContextAsync(); return await db.Suppliers.AsNoTracking().OrderBy(x => x.DisplayName).ToListAsync(); }
    /// <summary>Saves a supplier.</summary>
    public async Task SaveSupplierAsync(Supplier item) { EnsureManager(); ValidationHelper.ValidatePartner(item.DisplayName, item.Email, item.Phone); await SaveAsync(item, item.SupplierId == 0); }
    /// <summary>Deletes an unused supplier.</summary>
    public Task DeleteSupplierAsync(int id) { EnsureManager(); return DeleteAsync<Supplier>(id, "The supplier is used by a product."); }
    /// <summary>Loads customers.</summary>
    public async Task<List<Customer>> GetCustomersAsync() { await using var db = await factory.CreateDbContextAsync(); return await db.Customers.AsNoTracking().OrderBy(x => x.DisplayName).ToListAsync(); }
    /// <summary>Saves a customer.</summary>
    public async Task SaveCustomerAsync(Customer item) { EnsureManager(); ValidationHelper.ValidatePartner(item.DisplayName, item.Email, item.Phone); await SaveAsync(item, item.CustomerId == 0); }
    /// <summary>Deletes an unused customer.</summary>
    public Task DeleteCustomerAsync(int id) { EnsureManager(); return DeleteAsync<Customer>(id, "The customer is used by an export."); }
    /// <summary>Loads products with their unit and supplier.</summary>
    public async Task<List<Product>> GetProductsAsync() { await using var db = await factory.CreateDbContextAsync(); return await db.Objects.AsNoTracking().Include(x => x.Unit).Include(x => x.Supplier).OrderBy(x => x.DisplayName).ToListAsync(); }
    /// <summary>Saves a product while enforcing mandatory foreign keys.</summary>
    public async Task SaveProductAsync(Product item) { EnsureManager(); ValidationHelper.Require(item.Id, "Product ID"); ValidationHelper.Require(item.DisplayName, "Product name"); if (item.UnitId < 1 || item.SupplierId < 1) throw new InvalidOperationException("Select a unit and supplier."); await using var db = await factory.CreateDbContextAsync(); db.Entry(item).State = await db.Objects.AnyAsync(x => x.Id == item.Id) ? EntityState.Modified : EntityState.Added; await db.SaveChangesAsync(); }
    /// <summary>Deletes a product without transaction history.</summary>
    public Task DeleteProductAsync(string id) { EnsureManager(); return DeleteAsync<Product>(id, "The product has transaction history."); }
    /// <summary>Loads roles.</summary>
    public async Task<List<Role>> GetRolesAsync() { EnsureAdministrator(); await using var db = await factory.CreateDbContextAsync(); return await db.Roles.AsNoTracking().OrderBy(x => x.DisplayName).ToListAsync(); }
    /// <summary>Saves a role.</summary>
    public async Task SaveRoleAsync(Role item) { EnsureAdministrator(); ValidationHelper.Require(item.DisplayName, "Role name"); await SaveAsync(item, item.RoleId == 0); }
    /// <summary>Deletes an unassigned role.</summary>
    public Task DeleteRoleAsync(int id) { EnsureAdministrator(); return DeleteAsync<Role>(id, "The role is assigned to a user."); }
    /// <summary>Loads users and assigned roles.</summary>
    public async Task<List<UserAccount>> GetUsersAsync() { EnsureAdministrator(); await using var db = await factory.CreateDbContextAsync(); return await db.Users.AsNoTracking().Include(x => x.Role).OrderBy(x => x.DisplayName).ToListAsync(); }
    /// <summary>Saves a user and hashes a newly supplied password.</summary>
    public async Task SaveUserAsync(UserAccount item, string? password) { EnsureAdministrator(); ValidationHelper.Require(item.DisplayName, "Display name"); ValidationHelper.Require(item.UserName, "Username"); if (item.RoleId < 1) throw new InvalidOperationException("Select a role."); await using var db = await factory.CreateDbContextAsync(); if (await db.Users.AnyAsync(x => x.UserName == item.UserName && x.UserId != item.UserId)) throw new InvalidOperationException("Username already exists."); if (item.UserId == 0) { ValidationHelper.Require(password, "Password"); item.Password = SecurityHelper.HashPassword(password!); db.Add(item); } else { var stored = await db.Users.FindAsync(item.UserId) ?? throw new InvalidOperationException("User not found."); stored.DisplayName = item.DisplayName; stored.UserName = item.UserName; stored.RoleId = item.RoleId; if (!string.IsNullOrWhiteSpace(password)) stored.Password = SecurityHelper.HashPassword(password); } await db.SaveChangesAsync(); }
    /// <summary>Deletes a user.</summary>
    public Task DeleteUserAsync(int id) { EnsureAdministrator(); return DeleteAsync<UserAccount>(id, "The user could not be deleted."); }

    /// <summary>Creates an import header and all valid lines atomically.</summary>
    public async Task CreateInputAsync(IEnumerable<InputRequest> lines)
    {
        EnsureAuthenticated();
        var values = lines.ToList(); ValidateLines(values.Select(x => (x.ProductId, x.Count)).ToList()); if (values.Any(x => x.InputPrice < 0 || x.OutputPrice < 0)) throw new InvalidOperationException("Prices cannot be negative.");
        await using var db = await factory.CreateDbContextAsync(); var header = new Input { InputId = NewId("IN"), DateInput = DateTime.Now };
        header.InputInfos = values.Select(x => new InputInfo { Id = Guid.NewGuid().ToString("N"), ObjectId = x.ProductId, Count = x.Count, InputPrice = x.InputPrice, OutputPrice = x.OutputPrice, Status = "Completed" }).ToList(); db.Inputs.Add(header); await db.SaveChangesAsync();
    }

    /// <summary>Creates an export atomically after a serializable derived-stock check.</summary>
    public async Task CreateOutputAsync(int customerId, IEnumerable<OutputRequest> lines)
    {
        EnsureAuthenticated();
        if (customerId < 1) throw new InvalidOperationException("Select a customer."); var values = lines.ToList(); ValidateLines(values.Select(x => (x.ProductId, x.Count)).ToList());
        await using var db = await factory.CreateDbContextAsync(); await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        foreach (var line in values) { var input = await db.InputInfos.Where(x => x.ObjectId == line.ProductId).SumAsync(x => (int?)x.Count) ?? 0; var output = await db.OutputInfos.Where(x => x.ObjectId == line.ProductId).SumAsync(x => (int?)x.Count) ?? 0; if (line.Count > input - output) throw new InvalidOperationException($"Insufficient stock for {line.ProductId}. Available: {input - output}."); }
        var header = new Output { OutputId = NewId("OUT"), DateOutput = DateTime.Now }; header.OutputInfos = values.Select(x => new OutputInfo { Id = Guid.NewGuid().ToString("N"), ObjectId = x.ProductId, CustomerId = customerId, Count = x.Count, Status = "Completed" }).ToList(); db.Outputs.Add(header); await db.SaveChangesAsync(); await transaction.CommitAsync();
    }

    /// <summary>Computes stock from the immutable import/export ledger.</summary>
    public async Task<List<InventoryRow>> GetInventoryAsync(string? search)
    {
        EnsureManager();
        await using var db = await factory.CreateDbContextAsync(); var query = db.Objects.AsNoTracking(); if (!string.IsNullOrWhiteSpace(search)) query = query.Where(x => x.Id.Contains(search) || (x.DisplayName ?? "").Contains(search));
        return await query.Select(x => new InventoryRow(x.Id, x.DisplayName ?? "", x.Unit.DisplayName ?? "", x.Supplier.DisplayName ?? "", x.InputInfos.Sum(i => (int?)i.Count) ?? 0, x.OutputInfos.Sum(o => (int?)o.Count) ?? 0, (x.InputInfos.Sum(i => (int?)i.Count) ?? 0) - (x.OutputInfos.Sum(o => (int?)o.Count) ?? 0))).OrderBy(x => x.Product).ToListAsync();
    }

    /// <summary>Persists a master-data entity.</summary>
    private async Task SaveAsync<T>(T item, bool added) where T : class { await using var db = await factory.CreateDbContextAsync(); db.Entry(item).State = added ? EntityState.Added : EntityState.Modified; await db.SaveChangesAsync(); }
    /// <summary>Deletes an entity and translates referential-integrity failures.</summary>
    private async Task DeleteAsync<T>(object id, string message) where T : class { await using var db = await factory.CreateDbContextAsync(); var item = await db.Set<T>().FindAsync(id) ?? throw new InvalidOperationException("Record not found."); db.Remove(item); try { await db.SaveChangesAsync(); } catch (DbUpdateException) { throw new InvalidOperationException(message); } }
    /// <summary>Validates line presence, positive quantity, and duplicate products.</summary>
    private static void ValidateLines(IReadOnlyCollection<(string ProductId, int Count)> lines) { if (lines.Count == 0 || lines.Any(x => string.IsNullOrWhiteSpace(x.ProductId) || x.Count <= 0)) throw new InvalidOperationException("Add at least one product with a positive quantity."); if (lines.GroupBy(x => x.ProductId).Any(x => x.Count() > 1)) throw new InvalidOperationException("A product may appear only once per receipt."); }
    /// <summary>Generates a readable transaction key below the 128-character limit.</summary>
    private static string NewId(string prefix) => $"{prefix}-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}";
    /// <summary>Requires an authenticated session.</summary>
    private void EnsureAuthenticated() { if (currentUser is null) throw new UnauthorizedAccessException("Login is required."); }
    /// <summary>Requires a manager or administrator session.</summary>
    private void EnsureManager() { EnsureAuthenticated(); var role = currentUser!.Role.DisplayName ?? ""; if (!role.Contains("Manager", StringComparison.OrdinalIgnoreCase) && !role.Contains("Admin", StringComparison.OrdinalIgnoreCase)) throw new UnauthorizedAccessException("Manager permission is required."); }
    /// <summary>Requires an administrator session.</summary>
    private void EnsureAdministrator() { EnsureAuthenticated(); if (!(currentUser!.Role.DisplayName ?? "").Contains("Admin", StringComparison.OrdinalIgnoreCase)) throw new UnauthorizedAccessException("Administrator permission is required."); }
}
