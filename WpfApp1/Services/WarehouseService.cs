using System.IO;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RalseiWarehouse.Data;
using RalseiWarehouse.Models;
using Product = RalseiWarehouse.Models.Object;

namespace RalseiWarehouse.Services;

/// <summary>
/// Singleton service providing all warehouse management operations including
/// master data CRUD, stock transactions, inventory, reporting, file export, and JSON serialization.
/// </summary>
public class WarehouseService
{
    private static WarehouseService? _instance;
    private static readonly object _lock = new();
    private readonly string _connectionString;

    private WarehouseService(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = connectionString;
    }

    /// <summary>
    /// Gets the singleton instance of the warehouse service.
    /// </summary>
    /// <param name="connectionString">The SQL Server connection string.</param>
    /// <returns>The singleton <see cref="WarehouseService"/> instance.</returns>
    public static WarehouseService GetInstance(string connectionString)
    {
        if (_instance is null)
        {
            lock (_lock)
            {
                _instance ??= new WarehouseService(connectionString);
            }
        }
        return _instance;
    }

    private WarehouseDbContext CreateDb() => new(_connectionString);

    /// <summary>
    /// Ensures the database exists and seeds default roles and users if empty.
    /// </summary>
    public async Task EnsureDatabaseAsync()
    {
        await using var db = CreateDb();
        try
        {
            _ = await db.Users.AnyAsync();
        }
        catch
        {
            await db.Database.EnsureDeletedAsync();
        }
        await db.Database.EnsureCreatedAsync();

        if (!await db.Roles.AnyAsync())
        {
            db.Roles.Add(new Role { DisplayName = "Admin" });
            db.Roles.Add(new Role { DisplayName = "Staff" });
            await db.SaveChangesAsync();
        }

        if (!await db.Users.AnyAsync())
        {
            var adminRole = await db.Roles.FirstAsync(r => r.DisplayName == "Admin");
            var staffRole = await db.Roles.FirstAsync(r => r.DisplayName == "Staff");
            db.Users.Add(new User { UserName = "admin", Password = "admin", DisplayName = "Administrator", RoleId = adminRole.RoleId });
            db.Users.Add(new User { UserName = "staff", Password = "staff", DisplayName = "Staff", RoleId = staffRole.RoleId });
            await db.SaveChangesAsync();
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  AUTHENTICATION
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Authenticates a user with plain-text username and password.
    /// </summary>
    /// <param name="username">The username.</param>
    /// <param name="password">The plain-text password.</param>
    /// <returns>The authenticated <see cref="User"/> with role loaded, or null.</returns>
    public async Task<User?> AuthenticateAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
            return null;

        await using var db = CreateDb();
        return await db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserName == username && u.Password == password);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  MASTER DATA: UNITS
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Retrieves all units ordered by name.</summary>
    public async Task<List<Unit>> GetUnitsAsync()
    {
        await using var db = CreateDb();
        return await db.Units.OrderBy(u => u.DisplayName).ToListAsync();
    }

    /// <summary>Creates or updates a unit.</summary>
    public async Task SaveUnitAsync(Unit unit)
    {
        ArgumentNullException.ThrowIfNull(unit);
        if (string.IsNullOrWhiteSpace(unit.DisplayName))
            throw new ArgumentException("Unit name is required.", nameof(unit));

        await using var db = CreateDb();
        if (unit.UnitId == 0)
            db.Units.Add(unit);
        else
        {
            var existing = await db.Units.FindAsync(unit.UnitId)
                ?? throw new InvalidOperationException("Unit not found.");
            existing.DisplayName = unit.DisplayName;
        }
        await db.SaveChangesAsync();
    }

    /// <summary>Deletes a unit by identifier.</summary>
    public async Task DeleteUnitAsync(int id)
    {
        await using var db = CreateDb();
        var item = await db.Units.FindAsync(id);
        if (item is not null)
        {
            db.Units.Remove(item);
            await db.SaveChangesAsync();
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  MASTER DATA: SUPPLIERS
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Retrieves all suppliers ordered by name.</summary>
    public async Task<List<Supplier>> GetSuppliersAsync()
    {
        await using var db = CreateDb();
        return await db.Suppliers.OrderBy(s => s.DisplayName).ToListAsync();
    }

    /// <summary>Creates or updates a supplier.</summary>
    public async Task SaveSupplierAsync(Supplier supplier)
    {
        ArgumentNullException.ThrowIfNull(supplier);
        if (string.IsNullOrWhiteSpace(supplier.DisplayName))
            throw new ArgumentException("Supplier name is required.", nameof(supplier));

        await using var db = CreateDb();
        if (supplier.SupplierId == 0)
            db.Suppliers.Add(supplier);
        else
        {
            var existing = await db.Suppliers.FindAsync(supplier.SupplierId)
                ?? throw new InvalidOperationException("Supplier not found.");
            existing.DisplayName = supplier.DisplayName;
            existing.Address = supplier.Address;
            existing.Phone = supplier.Phone;
            existing.Email = supplier.Email;
            existing.MoreInfo = supplier.MoreInfo;
            existing.ContractDate = supplier.ContractDate;
        }
        await db.SaveChangesAsync();
    }

    /// <summary>Deletes a supplier by identifier.</summary>
    public async Task DeleteSupplierAsync(int id)
    {
        await using var db = CreateDb();
        var item = await db.Suppliers.FindAsync(id);
        if (item is not null)
        {
            db.Suppliers.Remove(item);
            await db.SaveChangesAsync();
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  MASTER DATA: CUSTOMERS
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Retrieves all customers ordered by name.</summary>
    public async Task<List<Customer>> GetCustomersAsync()
    {
        await using var db = CreateDb();
        return await db.Customers.OrderBy(c => c.DisplayName).ToListAsync();
    }

    /// <summary>Creates or updates a customer.</summary>
    public async Task SaveCustomerAsync(Customer customer)
    {
        ArgumentNullException.ThrowIfNull(customer);
        if (string.IsNullOrWhiteSpace(customer.DisplayName))
            throw new ArgumentException("Customer name is required.", nameof(customer));

        await using var db = CreateDb();
        if (customer.CustomerId == 0)
            db.Customers.Add(customer);
        else
        {
            var existing = await db.Customers.FindAsync(customer.CustomerId)
                ?? throw new InvalidOperationException("Customer not found.");
            existing.DisplayName = customer.DisplayName;
            existing.Address = customer.Address;
            existing.Phone = customer.Phone;
            existing.Email = customer.Email;
            existing.MoreInfo = customer.MoreInfo;
            existing.ContractDate = customer.ContractDate;
        }
        await db.SaveChangesAsync();
    }

    /// <summary>Deletes a customer by identifier.</summary>
    public async Task DeleteCustomerAsync(int id)
    {
        await using var db = CreateDb();
        var item = await db.Customers.FindAsync(id);
        if (item is not null)
        {
            db.Customers.Remove(item);
            await db.SaveChangesAsync();
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  MASTER DATA: PRODUCTS (Objects)
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Retrieves all products with unit and supplier loaded, ordered by name.</summary>
    public async Task<List<Product>> GetProductsAsync()
    {
        await using var db = CreateDb();
        return await db.Objects
            .Include(o => o.Unit)
            .Include(o => o.Supplier)
            .OrderBy(o => o.DisplayName)
            .ToListAsync();
    }

    /// <summary>Creates or updates a product.</summary>
    public async Task SaveProductAsync(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);
        if (string.IsNullOrWhiteSpace(product.Id))
            throw new ArgumentException("Product ID is required.", nameof(product));
        if (string.IsNullOrWhiteSpace(product.DisplayName))
            throw new ArgumentException("Product name is required.", nameof(product));

        await using var db = CreateDb();
        var existing = await db.Objects.FindAsync(product.Id);
        if (existing is null)
            db.Objects.Add(product);
        else
        {
            existing.DisplayName = product.DisplayName;
            existing.UnitId = product.UnitId;
            existing.SupplierId = product.SupplierId;
            existing.QRCode = product.QRCode;
            existing.BarCode = product.BarCode;
        }
        await db.SaveChangesAsync();
    }

    /// <summary>Deletes a product by identifier.</summary>
    public async Task DeleteProductAsync(string id)
    {
        await using var db = CreateDb();
        var item = await db.Objects.FindAsync(id);
        if (item is not null)
        {
            db.Objects.Remove(item);
            await db.SaveChangesAsync();
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  TRANSACTIONS: IMPORT (Stock In)
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates an import receipt with line items.
    /// </summary>
    /// <param name="lines">The import line items (product, quantity, prices).</param>
    /// <exception cref="InvalidOperationException">Thrown when validation fails.</exception>
    public async Task CreateImportAsync(List<ImportLine> lines)
    {
        if (lines.Count == 0)
            throw new InvalidOperationException("Add at least one product.");
        if (lines.Any(l => l.Product is null || l.Count <= 0))
            throw new InvalidOperationException("Select a product with positive quantity for each line.");
        if (lines.Any(l => l.InputPrice < 0 || l.OutputPrice < 0))
            throw new InvalidOperationException("Prices cannot be negative.");
        if (lines.GroupBy(l => l.Product!.Id).Any(g => g.Count() > 1))
            throw new InvalidOperationException("A product may appear only once per receipt.");

        await using var db = CreateDb();
        var header = new Input
        {
            InputId = $"IN-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}",
            DateInput = DateTime.Now,
            InputInfos = lines.Select(l => new InputInfo
            {
                Id = Guid.NewGuid().ToString("N"),
                ObjectId = l.Product!.Id,
                Count = l.Count,
                InputPrice = l.InputPrice,
                OutputPrice = l.OutputPrice,
                Status = "Completed"
            }).ToList()
        };

        db.Inputs.Add(header);
        await db.SaveChangesAsync();
    }

    // ═══════════════════════════════════════════════════════════════════
    //  TRANSACTIONS: EXPORT (Stock Out)
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates an export receipt with line items, validating stock availability.
    /// </summary>
    /// <param name="customerId">The customer identifier.</param>
    /// <param name="lines">The export line items (product, quantity).</param>
    /// <exception cref="InvalidOperationException">Thrown when validation fails or stock is insufficient.</exception>
    public async Task CreateExportAsync(int customerId, List<ExportLine> lines)
    {
        if (customerId < 1)
            throw new InvalidOperationException("Select a customer.");
        if (lines.Count == 0)
            throw new InvalidOperationException("Add at least one product.");
        if (lines.Any(l => l.Product is null || l.Count <= 0))
            throw new InvalidOperationException("Select a product with positive quantity for each line.");
        if (lines.GroupBy(l => l.Product!.Id).Any(g => g.Count() > 1))
            throw new InvalidOperationException("A product may appear only once per receipt.");

        await using var db = CreateDb();

        foreach (var line in lines)
        {
            var inputSum = await db.InputInfos
                .Where(i => i.ObjectId == line.Product!.Id).SumAsync(i => (int?)i.Count) ?? 0;
            var outputSum = await db.OutputInfos
                .Where(o => o.ObjectId == line.Product!.Id).SumAsync(o => (int?)o.Count) ?? 0;
            var stock = inputSum - outputSum;
            if (line.Count > stock)
                throw new InvalidOperationException(
                    $"Insufficient stock for {line.Product!.DisplayName}. Available: {stock}.");
        }

        var header = new Output
        {
            OutputId = $"OUT-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}",
            DateOutput = DateTime.Now,
            OutputInfos = lines.Select(l => new OutputInfo
            {
                Id = Guid.NewGuid().ToString("N"),
                ObjectId = l.Product!.Id,
                CustomerId = customerId,
                Count = l.Count,
                Status = "Completed"
            }).ToList()
        };

        db.Outputs.Add(header);
        await db.SaveChangesAsync();
    }

    /// <summary>Retrieves recent import receipts.</summary>
    public async Task<List<Input>> GetRecentImportsAsync()
    {
        await using var db = CreateDb();
        return await db.Inputs.Include(i => i.InputInfos)
            .OrderByDescending(i => i.DateInput).Take(50).ToListAsync();
    }

    /// <summary>Retrieves recent export receipts.</summary>
    public async Task<List<Output>> GetRecentExportsAsync()
    {
        await using var db = CreateDb();
        return await db.Outputs.Include(o => o.OutputInfos)
            .OrderByDescending(o => o.DateOutput).Take(50).ToListAsync();
    }

    // ═══════════════════════════════════════════════════════════════════
    //  INVENTORY (computed: total input - total output per product)
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Generates an inventory report with computed stock levels.</summary>
    public async Task<List<object>> GetInventoryReportAsync(string? search)
    {
        await using var db = CreateDb();

        var query = db.Objects.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(o => o.Id.Contains(search) || (o.DisplayName ?? "").Contains(search));

        var data = await query
            .Include(o => o.Unit).Include(o => o.Supplier)
            .OrderBy(o => o.DisplayName)
            .Select(o => new
            {
                o.Id,
                Product = o.DisplayName ?? "",
                Unit = o.Unit!.DisplayName ?? "",
                Supplier = o.Supplier!.DisplayName ?? "",
                TotalIn = o.InputInfos.Sum(i => (int?)i.Count) ?? 0,
                TotalOut = o.OutputInfos.Sum(o2 => (int?)o2.Count) ?? 0,
            })
            .ToListAsync();

        return data.Select(d => (object)new
        {
            d.Id, d.Product, d.Unit, d.Supplier, d.TotalIn, d.TotalOut,
            Stock = d.TotalIn - d.TotalOut
        }).ToList();
    }

    // ═══════════════════════════════════════════════════════════════════
    //  ADMINISTRATION: ROLES
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Retrieves all roles ordered by name.</summary>
    public async Task<List<Role>> GetRolesAsync()
    {
        await using var db = CreateDb();
        return await db.Roles.OrderBy(r => r.DisplayName).ToListAsync();
    }

    /// <summary>Creates or updates a role.</summary>
    public async Task SaveRoleAsync(Role role)
    {
        ArgumentNullException.ThrowIfNull(role);
        if (string.IsNullOrWhiteSpace(role.DisplayName))
            throw new ArgumentException("Role name is required.", nameof(role));

        await using var db = CreateDb();
        if (role.RoleId == 0)
            db.Roles.Add(role);
        else
        {
            var existing = await db.Roles.FindAsync(role.RoleId)
                ?? throw new InvalidOperationException("Role not found.");
            existing.DisplayName = role.DisplayName;
        }
        await db.SaveChangesAsync();
    }

    /// <summary>Deletes a role by identifier.</summary>
    public async Task DeleteRoleAsync(int id)
    {
        await using var db = CreateDb();
        var item = await db.Roles.FindAsync(id);
        if (item is not null)
        {
            db.Roles.Remove(item);
            await db.SaveChangesAsync();
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  ADMINISTRATION: USERS
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Retrieves all users with roles loaded, ordered by display name.</summary>
    public async Task<List<User>> GetUsersAsync()
    {
        await using var db = CreateDb();
        return await db.Users.Include(u => u.Role).OrderBy(u => u.DisplayName).ToListAsync();
    }

    /// <summary>Creates or updates a user account.</summary>
    public async Task SaveUserAsync(User user, bool isNew)
    {
        ArgumentNullException.ThrowIfNull(user);
        if (string.IsNullOrWhiteSpace(user.UserName))
            throw new ArgumentException("Username is required.", nameof(user));
        if (string.IsNullOrWhiteSpace(user.DisplayName))
            throw new ArgumentException("Display name is required.", nameof(user));

        await using var db = CreateDb();
        if (isNew)
        {
            if (string.IsNullOrWhiteSpace(user.Password))
                throw new ArgumentException("Password is required for new users.", nameof(user));
            if (await db.Users.AnyAsync(u => u.UserName == user.UserName))
                throw new InvalidOperationException("Username already exists.");
            db.Users.Add(user);
        }
        else
        {
            var existing = await db.Users.FindAsync(user.UserId)
                ?? throw new InvalidOperationException("User not found.");
            if (await db.Users.AnyAsync(x => x.UserName == user.UserName && x.UserId != user.UserId))
                throw new InvalidOperationException("Username already exists.");
            existing.UserName = user.UserName;
            existing.DisplayName = user.DisplayName;
            existing.RoleId = user.RoleId;
            if (!string.IsNullOrWhiteSpace(user.Password))
                existing.Password = user.Password;
        }
        await db.SaveChangesAsync();
    }

    /// <summary>Deletes a user by identifier.</summary>
    public async Task DeleteUserAsync(int id)
    {
        await using var db = CreateDb();
        var item = await db.Users.FindAsync(id);
        if (item is not null)
        {
            db.Users.Remove(item);
            await db.SaveChangesAsync();
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  FILE EXPORT  (Ch09 System.IO)
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Exports the product list to a CSV file.</summary>
    public async Task<string> ExportToCsvAsync(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        await using var db = CreateDb();
        var products = await db.Objects
            .Include(o => o.Unit).Include(o => o.Supplier)
            .OrderBy(o => o.DisplayName).ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Id,Name,Unit,Supplier,QRCode,BarCode");
        foreach (var p in products)
            sb.AppendLine($"{p.Id},{p.DisplayName},{p.Unit?.DisplayName},{p.Supplier?.DisplayName},{p.QRCode},{p.BarCode}");

        await File.WriteAllTextAsync(filePath, sb.ToString());
        return filePath;
    }

    /// <summary>Exports a formatted inventory report to a text file.</summary>
    public async Task<string> ExportReportToFileAsync(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        await using var db = CreateDb();
        var data = await db.Objects
            .Include(o => o.Unit).Include(o => o.Supplier)
            .OrderBy(o => o.DisplayName)
            .Select(o => new
            {
                o.Id, Name = o.DisplayName ?? "",
                Unit = o.Unit!.DisplayName ?? "",
                Supplier = o.Supplier!.DisplayName ?? "",
                In = o.InputInfos.Sum(i => (int?)i.Count) ?? 0,
                Out = o.OutputInfos.Sum(o2 => (int?)o2.Count) ?? 0,
            }).ToListAsync();

        using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
        await writer.WriteLineAsync("WAREHOUSE INVENTORY REPORT");
        await writer.WriteLineAsync($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        await writer.WriteLineAsync(new string('-', 70));
        await writer.WriteLineAsync(string.Format("{0,-15} {1,-25} {2,6} {3,6} {4,6}", "ID", "Product", "In", "Out", "Stock"));
        await writer.WriteLineAsync(new string('-', 70));

        foreach (var d in data)
            await writer.WriteLineAsync(string.Format("{0,-15} {1,-25} {2,6} {3,6} {4,6}", d.Id, d.Name, d.In, d.Out, d.In - d.Out));

        await writer.WriteLineAsync(new string('-', 70));
        await writer.WriteLineAsync($"Total Products: {data.Count}");
        await writer.WriteLineAsync($"Total Stock: {data.Sum(d => d.In - d.Out)}");

        return filePath;
    }

    // ═══════════════════════════════════════════════════════════════════
    //  JSON EXPORT / IMPORT  (Ch10 JSON Serialization)
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Exports all products to a JSON file.</summary>
    public async Task<string> ExportToJsonAsync(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        await using var db = CreateDb();
        var products = await db.Objects
            .Include(o => o.Unit).Include(o => o.Supplier)
            .OrderBy(o => o.DisplayName).ToListAsync();

        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(products, options);
        await File.WriteAllTextAsync(filePath, json);
        return filePath;
    }

    /// <summary>Imports products from a JSON file, skipping duplicates by name.</summary>
    public async Task<int> ImportFromJsonAsync(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        var json = await File.ReadAllTextAsync(filePath);
        var products = JsonSerializer.Deserialize<List<Product>>(json)
            ?? throw new InvalidOperationException("Invalid JSON file.");

        await using var db = CreateDb();
        int count = 0;
        foreach (var p in products)
        {
            if (string.IsNullOrWhiteSpace(p.DisplayName)) continue;
            if (!await db.Objects.AnyAsync(x => x.DisplayName == p.DisplayName))
            {
                p.Unit = null;
                p.Supplier = null;
                p.InputInfos = new List<InputInfo>();
                p.OutputInfos = new List<OutputInfo>();
                db.Objects.Add(p);
                count++;
            }
        }
        await db.SaveChangesAsync();
        return count;
    }

    // ═══════════════════════════════════════════════════════════════════
    //  DASHBOARD STATS  (Ch06 LINQ - Count, Sum, Average, Min, Max)
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Computes dashboard statistics using LINQ aggregate operators.</summary>
    public async Task<Dictionary<string, object>> GetDashboardStatsAsync()
    {
        await using var db = CreateDb();
        var products = await db.Objects.ToListAsync();
        var inputs = await db.InputInfos.ToListAsync();
        var outputs = await db.OutputInfos.ToListAsync();

        var stats = new Dictionary<string, object>
        {
            ["Total Products"] = products.Count,
            ["Total Suppliers"] = await db.Suppliers.CountAsync(),
            ["Total Customers"] = await db.Customers.CountAsync(),
            ["Total Stock In"] = inputs.Sum(i => i.Count),
            ["Total Stock Out"] = outputs.Sum(o => o.Count),
            ["Current Stock"] = inputs.Sum(i => i.Count) - outputs.Sum(o => o.Count),
            ["Total Import Value"] = inputs.Sum(i => (i.InputPrice ?? 0) * i.Count),
            ["Total Export Value"] = outputs.Sum(o =>
            {
                var inp = inputs.FirstOrDefault(i => i.ObjectId == o.ObjectId);
                return (inp?.OutputPrice ?? 0) * o.Count;
            }),
            ["Transactions Today"] = (await db.Inputs.CountAsync(i => i.DateInput.HasValue && i.DateInput.Value.Date == DateTime.Today))
                                   + (await db.Outputs.CountAsync(o => o.DateOutput.HasValue && o.DateOutput.Value.Date == DateTime.Today)),
        };

        return stats;
    }
}

/// <summary>
/// Represents a line item for creating an import receipt.
/// </summary>
public class ImportLine
{
    /// <summary>Gets or sets the selected product.</summary>
    public Product? Product { get; set; }
    /// <summary>Gets or sets the quantity received.</summary>
    public int Count { get; set; } = 1;
    /// <summary>Gets or sets the purchase price per unit.</summary>
    public double InputPrice { get; set; }
    /// <summary>Gets or sets the selling price per unit.</summary>
    public double OutputPrice { get; set; }
}

/// <summary>
/// Represents a line item for creating an export receipt.
/// </summary>
public class ExportLine
{
    /// <summary>Gets or sets the selected product.</summary>
    public Product? Product { get; set; }
    /// <summary>Gets or sets the quantity shipped.</summary>
    public int Count { get; set; } = 1;
}
