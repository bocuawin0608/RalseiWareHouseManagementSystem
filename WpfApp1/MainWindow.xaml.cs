using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using RalseiWarehouse.Data;
using RalseiWarehouse.Models;
using Product = RalseiWarehouse.Models.Object;

namespace RalseiWarehouse;

public partial class MainWindow : Window
{
    // ── Session ──
    private readonly User _currentUser;
    private bool _isManager;
    private bool _isAdmin;

    // ── Master Data Collections ──
    private readonly ObservableCollection<Unit> _units = new();
    private readonly ObservableCollection<Supplier> _suppliers = new();
    private readonly ObservableCollection<Customer> _customers = new();
    private readonly ObservableCollection<Object> _products = new();

    // ── Transaction Collections ──
    private readonly ObservableCollection<Product> _productList = new();
    private readonly ObservableCollection<Customer> _customerList = new();
    private readonly ObservableCollection<ImportLine> _importLines = new();
    private readonly ObservableCollection<ExportLine> _exportLines = new();
    private readonly ObservableCollection<Input> _recentImports = new();
    private readonly ObservableCollection<Output> _recentExports = new();

    // Admin Collections
    private readonly ObservableCollection<Role> _roles = new();
    private readonly ObservableCollection<User> _users = new();

    public static readonly DependencyProperty ProductsProperty =
        DependencyProperty.Register("Products", typeof(ObservableCollection<Product>), typeof(MainWindow));
    public ObservableCollection<Product> Products => _productList;

    public MainWindow(User user)
    {
        InitializeComponent();
        _currentUser = user;

        var role = user.Role?.DisplayName ?? "";
        _isAdmin = role.Contains("Admin", StringComparison.OrdinalIgnoreCase);
        _isManager = _isAdmin || role.Contains("Manager", StringComparison.OrdinalIgnoreCase);

        txtUserName.Text = user.DisplayName ?? user.UserName;
        txtUserRole.Text = user.Role?.DisplayName ?? "";

        // Hide tabs based on role
        tabMasterData.Visibility = _isManager ? Visibility.Visible : Visibility.Collapsed;
        tabInventory.Visibility = _isManager ? Visibility.Visible : Visibility.Collapsed;
        tabAdministration.Visibility = _isAdmin ? Visibility.Visible : Visibility.Collapsed;

        // Bind DataGrids
        dgUnits.ItemsSource = _units;
        dgSuppliers.ItemsSource = _suppliers;
        dgCustomers.ItemsSource = _customers;
        dgProducts.ItemsSource = _products;
        dgImportLines.ItemsSource = _importLines;
        dgExportLines.ItemsSource = _exportLines;
        dgRecentImports.ItemsSource = _recentImports;
        dgRecentExports.ItemsSource = _recentExports;
        dgRoles.ItemsSource = _roles;
        dgUsers.ItemsSource = _users;

        // Product combos in transactions get their items from _productList
        cboProductUnit.ItemsSource = _units;
        cboProductSupplier.ItemsSource = _suppliers;
        cboExportCustomer.ItemsSource = _customerList;
        cboUserRole.ItemsSource = _roles;

        // Pre-load first tab
        if (_isManager) _ = LoadMasterDataAsync();
        _ = LoadTransactionDataAsync();
    }

    // ─── Helper: Create DbContext ────────────────────────────────────
    private WarehouseDbContext CreateDb() => new(App.ConnectionString);

    // ─── Helper: Show error ──────────────────────────────────────────
    private void ShowError(Exception ex)
    {
        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    // ─── Tab Switch: Load data when entering a tab ───────────────────
    private void TabMain_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.Source is TabControl tc && tc.SelectedItem is TabItem ti)
        {
            if (ti == tabMasterData && _units.Count == 0)
                _ = LoadMasterDataAsync();
            else if (ti == tabTransactions && _productList.Count == 0)
                _ = LoadTransactionDataAsync();
            else if (ti == tabInventory)
                _ = LoadInventoryAsync();
            else if (ti == tabAdministration && _roles.Count == 0)
                _ = LoadAdministrationAsync();
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  MASTER DATA
    // ═══════════════════════════════════════════════════════════════════

    private async Task LoadMasterDataAsync()
    {
        try
        {
            await using var db = CreateDb();

            var units = await db.Units.AsNoTracking().OrderBy(u => u.DisplayName).ToListAsync();
            var suppliers = await db.Suppliers.AsNoTracking().OrderBy(s => s.DisplayName).ToListAsync();
            var customers = await db.Customers.AsNoTracking().OrderBy(c => c.DisplayName).ToListAsync();
            var products = await db.Objects.AsNoTracking()
                .Include(o => o.Unit).Include(o => o.Supplier)
                .OrderBy(o => o.DisplayName).ToListAsync();

            _units.Clear(); foreach (var u in units) _units.Add(u);
            _suppliers.Clear(); foreach (var s in suppliers) _suppliers.Add(s);
            _customers.Clear(); foreach (var c in customers) _customers.Add(c);
            _products.Clear(); foreach (var p in products) _products.Add(p);

            // Also refresh product/supplier combos (for product edit form)
            cboProductUnit.ItemsSource = _units;
            cboProductSupplier.ItemsSource = _suppliers;
        }
        catch (Exception ex) { ShowError(ex); }
    }

    // ── Units ────────────────────────────────────────────────────────
    private Unit? _selectedUnit;

    private void DgUnits_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedUnit = dgUnits.SelectedItem as Unit;
        txtUnitName.Text = _selectedUnit?.DisplayName ?? "";
    }

    private void BtnNewUnit_Click(object sender, RoutedEventArgs e)
    {
        _selectedUnit = new Unit();
        txtUnitName.Text = "";
    }

    private async void BtnSaveUnit_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var name = txtUnitName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name)) { MessageBox.Show("Unit name is required."); return; }

            await using var db = CreateDb();
            if (_selectedUnit is not null && _selectedUnit.UnitId == 0)
            {
                _selectedUnit.DisplayName = name;
                db.Units.Add(_selectedUnit);
            }
            else if (_selectedUnit is not null)
            {
                var existing = await db.Units.FindAsync(_selectedUnit.UnitId);
                if (existing is not null) existing.DisplayName = name;
            }
            else { MessageBox.Show("Select or create a unit first."); return; }

            await db.SaveChangesAsync();
            await LoadMasterDataAsync();
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private async void BtnDeleteUnit_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_selectedUnit is null || _selectedUnit.UnitId == 0)
            { MessageBox.Show("Select a unit first."); return; }

            await using var db = CreateDb();
            var item = await db.Units.FindAsync(_selectedUnit.UnitId);
            if (item is null) return;
            db.Units.Remove(item);

            try { await db.SaveChangesAsync(); }
            catch (DbUpdateException) { throw new InvalidOperationException("This unit is used by a product."); }

            await LoadMasterDataAsync();
        }
        catch (Exception ex) { ShowError(ex); }
    }

    // ── Suppliers ────────────────────────────────────────────────────
    private Supplier? _selectedSupplier;

    private void DgSuppliers_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedSupplier = dgSuppliers.SelectedItem as Supplier;
        if (_selectedSupplier is null) return;
        txtSupplierName.Text = _selectedSupplier.DisplayName ?? "";
        txtSupplierAddr.Text = _selectedSupplier.Address ?? "";
        txtSupplierPhone.Text = _selectedSupplier.Phone ?? "";
        txtSupplierEmail.Text = _selectedSupplier.Email ?? "";
        txtSupplierNotes.Text = _selectedSupplier.MoreInfo ?? "";
        dpSupplierDate.SelectedDate = _selectedSupplier.ContractDate;
    }

    private void BtnNewSupplier_Click(object sender, RoutedEventArgs e)
    {
        _selectedSupplier = new Supplier { ContractDate = DateTime.Today };
        txtSupplierName.Text = "";
        txtSupplierAddr.Text = "";
        txtSupplierPhone.Text = "";
        txtSupplierEmail.Text = "";
        txtSupplierNotes.Text = "";
        dpSupplierDate.SelectedDate = DateTime.Today;
    }

    private async void BtnSaveSupplier_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var name = txtSupplierName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name)) { MessageBox.Show("Supplier name is required."); return; }

            await using var db = CreateDb();
            if (_selectedSupplier is not null && _selectedSupplier.SupplierId == 0)
            {
                _selectedSupplier.DisplayName = name;
                _selectedSupplier.Address = txtSupplierAddr.Text;
                _selectedSupplier.Phone = txtSupplierPhone.Text;
                _selectedSupplier.Email = txtSupplierEmail.Text;
                _selectedSupplier.MoreInfo = txtSupplierNotes.Text;
                _selectedSupplier.ContractDate = dpSupplierDate.SelectedDate;
                db.Suppliers.Add(_selectedSupplier);
            }
            else if (_selectedSupplier is not null)
            {
                var existing = await db.Suppliers.FindAsync(_selectedSupplier.SupplierId);
                if (existing is not null)
                {
                    existing.DisplayName = name;
                    existing.Address = txtSupplierAddr.Text;
                    existing.Phone = txtSupplierPhone.Text;
                    existing.Email = txtSupplierEmail.Text;
                    existing.MoreInfo = txtSupplierNotes.Text;
                    existing.ContractDate = dpSupplierDate.SelectedDate;
                }
            }
            else { MessageBox.Show("Select or create a supplier first."); return; }

            await db.SaveChangesAsync();
            await LoadMasterDataAsync();
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private async void BtnDeleteSupplier_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_selectedSupplier is null || _selectedSupplier.SupplierId == 0)
            { MessageBox.Show("Select a supplier first."); return; }

            await using var db = CreateDb();
            var item = await db.Suppliers.FindAsync(_selectedSupplier.SupplierId);
            if (item is null) return;
            db.Suppliers.Remove(item);
            try { await db.SaveChangesAsync(); }
            catch (DbUpdateException) { throw new InvalidOperationException("This supplier is used by a product."); }
            await LoadMasterDataAsync();
        }
        catch (Exception ex) { ShowError(ex); }
    }

    // ── Customers ────────────────────────────────────────────────────
    private Customer? _selectedCustomer;

    private void DgCustomers_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedCustomer = dgCustomers.SelectedItem as Customer;
        if (_selectedCustomer is null) return;
        txtCustomerName.Text = _selectedCustomer.DisplayName ?? "";
        txtCustomerAddr.Text = _selectedCustomer.Address ?? "";
        txtCustomerPhone.Text = _selectedCustomer.Phone ?? "";
        txtCustomerEmail.Text = _selectedCustomer.Email ?? "";
        txtCustomerNotes.Text = _selectedCustomer.MoreInfo ?? "";
        dpCustomerDate.SelectedDate = _selectedCustomer.ContractDate;
    }

    private void BtnNewCustomer_Click(object sender, RoutedEventArgs e)
    {
        _selectedCustomer = new Customer { ContractDate = DateTime.Today };
        txtCustomerName.Text = "";
        txtCustomerAddr.Text = "";
        txtCustomerPhone.Text = "";
        txtCustomerEmail.Text = "";
        txtCustomerNotes.Text = "";
        dpCustomerDate.SelectedDate = DateTime.Today;
    }

    private async void BtnSaveCustomer_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var name = txtCustomerName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name)) { MessageBox.Show("Customer name is required."); return; }

            await using var db = CreateDb();
            if (_selectedCustomer is not null && _selectedCustomer.CustomerId == 0)
            {
                _selectedCustomer.DisplayName = name;
                _selectedCustomer.Address = txtCustomerAddr.Text;
                _selectedCustomer.Phone = txtCustomerPhone.Text;
                _selectedCustomer.Email = txtCustomerEmail.Text;
                _selectedCustomer.MoreInfo = txtCustomerNotes.Text;
                _selectedCustomer.ContractDate = dpCustomerDate.SelectedDate;
                db.Customers.Add(_selectedCustomer);
            }
            else if (_selectedCustomer is not null)
            {
                var existing = await db.Customers.FindAsync(_selectedCustomer.CustomerId);
                if (existing is not null)
                {
                    existing.DisplayName = name;
                    existing.Address = txtCustomerAddr.Text;
                    existing.Phone = txtCustomerPhone.Text;
                    existing.Email = txtCustomerEmail.Text;
                    existing.MoreInfo = txtCustomerNotes.Text;
                    existing.ContractDate = dpCustomerDate.SelectedDate;
                }
            }
            else { MessageBox.Show("Select or create a customer first."); return; }

            await db.SaveChangesAsync();
            await LoadMasterDataAsync();
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private async void BtnDeleteCustomer_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_selectedCustomer is null || _selectedCustomer.CustomerId == 0)
            { MessageBox.Show("Select a customer first."); return; }

            await using var db = CreateDb();
            var item = await db.Customers.FindAsync(_selectedCustomer.CustomerId);
            if (item is null) return;
            db.Customers.Remove(item);
            try { await db.SaveChangesAsync(); }
            catch (DbUpdateException) { throw new InvalidOperationException("This customer has export history."); }
            await LoadMasterDataAsync();
        }
        catch (Exception ex) { ShowError(ex); }
    }

    // ── Products ─────────────────────────────────────────────────────
    private Object? _selectedProduct;

    private void DgProducts_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedProduct = dgProducts.SelectedItem as Object;
        if (_selectedProduct is null) return;
        txtProductId.Text = _selectedProduct.Id;
        txtProductName.Text = _selectedProduct.DisplayName ?? "";
        cboProductUnit.SelectedValue = _selectedProduct.UnitId;
        cboProductSupplier.SelectedValue = _selectedProduct.SupplierId;
        txtProductQR.Text = _selectedProduct.QRCode ?? "";
        txtProductBarcode.Text = _selectedProduct.BarCode ?? "";
    }

    private void BtnNewProduct_Click(object sender, RoutedEventArgs e)
    {
        _selectedProduct = new Object();
        txtProductId.Text = "";
        txtProductName.Text = "";
        cboProductUnit.SelectedIndex = -1;
        cboProductSupplier.SelectedIndex = -1;
        txtProductQR.Text = "";
        txtProductBarcode.Text = "";
    }

    private async void BtnSaveProduct_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var id = txtProductId.Text.Trim();
            var name = txtProductName.Text.Trim();
            if (string.IsNullOrWhiteSpace(id)) { MessageBox.Show("Product ID is required."); return; }
            if (string.IsNullOrWhiteSpace(name)) { MessageBox.Show("Product name is required."); return; }
            if (cboProductUnit.SelectedValue is not int unitId || unitId < 1)
            { MessageBox.Show("Select a unit."); return; }
            if (cboProductSupplier.SelectedValue is not int supplierId || supplierId < 1)
            { MessageBox.Show("Select a supplier."); return; }

            await using var db = CreateDb();
            var exists = await db.Objects.AnyAsync(o => o.Id == id);
            if (!exists)
            {
                db.Objects.Add(new Object
                {
                    Id = id, DisplayName = name, UnitId = unitId, SupplierId = supplierId,
                    QRCode = txtProductQR.Text, BarCode = txtProductBarcode.Text
                });
            }
            else
            {
                var existing = await db.Objects.FindAsync(id);
                if (existing is not null)
                {
                    existing.DisplayName = name; existing.UnitId = unitId;
                    existing.SupplierId = supplierId;
                    existing.QRCode = txtProductQR.Text; existing.BarCode = txtProductBarcode.Text;
                }
            }

            await db.SaveChangesAsync();
            await LoadMasterDataAsync();
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private async void BtnDeleteProduct_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_selectedProduct is null) { MessageBox.Show("Select a product first."); return; }
            await using var db = CreateDb();
            var item = await db.Objects.FindAsync(_selectedProduct.Id);
            if (item is not null) db.Objects.Remove(item);
            try { await db.SaveChangesAsync(); }
            catch (DbUpdateException) { throw new InvalidOperationException("This product has transaction history."); }
            await LoadMasterDataAsync();
        }
        catch (Exception ex) { ShowError(ex); }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  TRANSACTIONS
    // ═══════════════════════════════════════════════════════════════════

    private async Task LoadTransactionDataAsync()
    {
        try
        {
            await using var db = CreateDb();

            var products = await db.Objects.AsNoTracking().Include(o => o.Unit).Include(o => o.Supplier)
                .OrderBy(o => o.DisplayName).ToListAsync();
            var customers = await db.Customers.AsNoTracking().OrderBy(c => c.DisplayName).ToListAsync();
            var imports = await db.Inputs.Include(i => i.InputInfos).OrderByDescending(i => i.DateInput).Take(50).ToListAsync();
            var exports = await db.Outputs.Include(o => o.OutputInfos).OrderByDescending(o => o.DateOutput).Take(50).ToListAsync();

            _productList.Clear(); foreach (var p in products) _productList.Add(p);
            _customerList.Clear(); foreach (var c in customers) _customerList.Add(c);
            _recentImports.Clear(); foreach (var i in imports) _recentImports.Add(i);
            _recentExports.Clear(); foreach (var e in exports) _recentExports.Add(e);

            cboExportCustomer.ItemsSource = _customerList;
        }
        catch (Exception ex) { ShowError(ex); }
    }

    // ── Import ───────────────────────────────────────────────────────
    private void BtnAddImportLine_Click(object sender, RoutedEventArgs e)
    {
        _importLines.Add(new ImportLine());
    }

    private void BtnRemoveImportLine_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is ImportLine line)
            _importLines.Remove(line);
    }

    private async void BtnCompleteImport_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_importLines.Count == 0) { MessageBox.Show("Add at least one product."); return; }
            if (_importLines.Any(l => l.Product is null || l.Count <= 0))
            { MessageBox.Show("Select a product with positive quantity for each line."); return; }
            if (_importLines.GroupBy(l => l.Product!.Id).Any(g => g.Count() > 1))
            { MessageBox.Show("A product may appear only once per receipt."); return; }

            await using var db = CreateDb();
            var header = new Input
            {
                InputId = $"IN-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}",
                DateInput = DateTime.Now
            };
            header.InputInfos = _importLines.Select(l => new InputInfo
            {
                Id = Guid.NewGuid().ToString("N"),
                ObjectId = l.Product!.Id,
                Count = l.Count,
                InputPrice = l.InputPrice,
                OutputPrice = l.OutputPrice,
                Status = "Completed"
            }).ToList();

            db.Inputs.Add(header);
            await db.SaveChangesAsync();

            _importLines.Clear();
            await LoadTransactionDataAsync();
            txtTransMessage.Text = "Import saved successfully.";
        }
        catch (Exception ex) { txtTransMessage.Text = ex.Message; }
    }

    // ── Export ───────────────────────────────────────────────────────
    private void BtnAddExportLine_Click(object sender, RoutedEventArgs e)
    {
        _exportLines.Add(new ExportLine());
    }

    private void BtnRemoveExportLine_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is ExportLine line)
            _exportLines.Remove(line);
    }

    private async void BtnCompleteExport_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (cboExportCustomer.SelectedItem is not Customer customer)
            { MessageBox.Show("Select a customer."); return; }
            if (_exportLines.Count == 0) { MessageBox.Show("Add at least one product."); return; }
            if (_exportLines.Any(l => l.Product is null || l.Count <= 0))
            { MessageBox.Show("Select a product with positive quantity for each line."); return; }
            if (_exportLines.GroupBy(l => l.Product!.Id).Any(g => g.Count() > 1))
            { MessageBox.Show("A product may appear only once per receipt."); return; }

            await using var db = CreateDb();

            // Stock check
            foreach (var line in _exportLines)
            {
                var inputSum = await db.InputInfos.Where(i => i.ObjectId == line.Product!.Id).SumAsync(i => (int?)i.Count) ?? 0;
                var outputSum = await db.OutputInfos.Where(o => o.ObjectId == line.Product!.Id).SumAsync(o => (int?)o.Count) ?? 0;
                var stock = inputSum - outputSum;
                if (line.Count > stock)
                    throw new InvalidOperationException($"Insufficient stock for {line.Product!.DisplayName}. Available: {stock}.");
            }

            var header = new Output
            {
                OutputId = $"OUT-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}",
                DateOutput = DateTime.Now
            };
            header.OutputInfos = _exportLines.Select(l => new OutputInfo
            {
                Id = Guid.NewGuid().ToString("N"),
                ObjectId = l.Product!.Id,
                CustomerId = customer.CustomerId,
                Count = l.Count,
                Status = "Completed"
            }).ToList();

            db.Outputs.Add(header);
            await db.SaveChangesAsync();

            _exportLines.Clear();
            await LoadTransactionDataAsync();
            txtTransMessage.Text = "Export saved successfully.";
        }
        catch (Exception ex) { txtTransMessage.Text = ex.Message; }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  INVENTORY (computed: input - output)
    // ═══════════════════════════════════════════════════════════════════

    private async Task LoadInventoryAsync()
    {
        try
        {
            var search = txtInventorySearch.Text.Trim();
            await using var db = CreateDb();

            var data = await db.Objects.AsNoTracking()
                .Include(o => o.Unit).Include(o => o.Supplier)
                .Where(o => string.IsNullOrEmpty(search) ||
                            o.Id.Contains(search) || (o.DisplayName ?? "").Contains(search))
                .OrderBy(o => o.DisplayName)
                .Select(o => new
                {
                    o.Id, Product = o.DisplayName ?? "",
                    Unit = o.Unit.DisplayName ?? "", Supplier = o.Supplier.DisplayName ?? "",
                    Input = o.InputInfos.Sum(i => (int?)i.Count) ?? 0,
                    Output = o.OutputInfos.Sum(o2 => (int?)o2.Count) ?? 0
                })
                .ToListAsync();

            var rows = data.Select(d => new
            {
                d.Id, d.Product, d.Unit, d.Supplier, d.Input, d.Output, Stock = d.Input - d.Output
            }).ToList();

            dgInventory.ItemsSource = rows;
        }
        catch (Exception ex) { txtInventoryMessage.Text = ex.Message; }
    }

    private async void BtnInventoryRefresh_Click(object sender, RoutedEventArgs e) => await LoadInventoryAsync();
    private async void TxtInventorySearch_TextChanged(object sender, TextChangedEventArgs e) => await LoadInventoryAsync();

    // ═══════════════════════════════════════════════════════════════════
    //  ADMINISTRATION
    // ═══════════════════════════════════════════════════════════════════

    private async Task LoadAdministrationAsync()
    {
        try
        {
            await using var db = CreateDb();
            var roles = await db.Roles.AsNoTracking().OrderBy(r => r.DisplayName).ToListAsync();
            var users = await db.Users.AsNoTracking().Include(u => u.Role).OrderBy(u => u.DisplayName).ToListAsync();

            _roles.Clear(); foreach (var r in roles) _roles.Add(r);
            _users.Clear(); foreach (var u in users) _users.Add(u);
            cboUserRole.ItemsSource = _roles;
        }
        catch (Exception ex) { ShowError(ex); }
    }

    // ── Roles ────────────────────────────────────────────────────────
    private Role? _selectedRole;

    private void DgRoles_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedRole = dgRoles.SelectedItem as Role;
        txtRoleName.Text = _selectedRole?.DisplayName ?? "";
    }

    private void BtnNewRole_Click(object sender, RoutedEventArgs e)
    {
        _selectedRole = new Role();
        txtRoleName.Text = "";
    }

    private async void BtnSaveRole_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var name = txtRoleName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name)) { MessageBox.Show("Role name is required."); return; }

            await using var db = CreateDb();
            if (_selectedRole is not null && _selectedRole.RoleId == 0)
            {
                _selectedRole.DisplayName = name;
                db.Roles.Add(_selectedRole);
            }
            else if (_selectedRole is not null)
            {
                var existing = await db.Roles.FindAsync(_selectedRole.RoleId);
                if (existing is not null) existing.DisplayName = name;
            }
            else { MessageBox.Show("Select or create a role first."); return; }
            await db.SaveChangesAsync();
            await LoadAdministrationAsync();
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private async void BtnDeleteRole_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_selectedRole is null || _selectedRole.RoleId == 0)
            { MessageBox.Show("Select a role first."); return; }
            await using var db = CreateDb();
            var item = await db.Roles.FindAsync(_selectedRole.RoleId);
            if (item is not null) db.Roles.Remove(item);
            await db.SaveChangesAsync();
            await LoadAdministrationAsync();
        }
        catch (Exception ex) { ShowError(ex); }
    }

    // ── Users ────────────────────────────────────────────────────────
    private User? _selectedUser;

    private void DgUsers_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedUser = dgUsers.SelectedItem as User;
        if (_selectedUser is null) return;
        txtUserDisplayName.Text = _selectedUser.DisplayName ?? "";
        txtUserUsername.Text = _selectedUser.UserName ?? "";
        cboUserRole.SelectedValue = _selectedUser.RoleId;
        txtUserPassword.Text = "";
    }

    private void BtnNewUser_Click(object sender, RoutedEventArgs e)
    {
        _selectedUser = new User();
        txtUserDisplayName.Text = "";
        txtUserUsername.Text = "";
        cboUserRole.SelectedIndex = -1;
        txtUserPassword.Text = "";
    }

    private async void BtnSaveUser_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var displayName = txtUserDisplayName.Text.Trim();
            var username = txtUserUsername.Text.Trim();
            var password = txtUserPassword.Text;

            if (string.IsNullOrWhiteSpace(displayName)) { MessageBox.Show("Display name is required."); return; }
            if (string.IsNullOrWhiteSpace(username)) { MessageBox.Show("Username is required."); return; }
            if (cboUserRole.SelectedValue is not int roleId || roleId < 1)
            { MessageBox.Show("Select a role."); return; }

            await using var db = CreateDb();

            if (_selectedUser is not null && _selectedUser.UserId == 0)
            {
                if (string.IsNullOrWhiteSpace(password)) { MessageBox.Show("Password is required for new users."); return; }
                if (await db.Users.AnyAsync(u => u.UserName == username))
                { MessageBox.Show("Username already exists."); return; }
                db.Users.Add(new User
                {
                    DisplayName = displayName, UserName = username,
                    Password = password, RoleId = roleId
                });
            }
            else if (_selectedUser is not null)
            {
                var existing = await db.Users.FindAsync(_selectedUser.UserId);
                if (existing is null) { MessageBox.Show("User not found."); return; }
                if (await db.Users.AnyAsync(u => u.UserName == username && u.UserId != existing.UserId))
                { MessageBox.Show("Username already exists."); return; }
                existing.DisplayName = displayName;
                existing.UserName = username;
                existing.RoleId = roleId;
                if (!string.IsNullOrWhiteSpace(password))
                    existing.Password = password;
            }
            else { MessageBox.Show("Select or create a user first."); return; }

            await db.SaveChangesAsync();
            await LoadAdministrationAsync();
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private async void BtnDeleteUser_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_selectedUser is null || _selectedUser.UserId == 0)
            { MessageBox.Show("Select a user first."); return; }
            await using var db = CreateDb();
            var item = await db.Users.FindAsync(_selectedUser.UserId);
            if (item is not null) db.Users.Remove(item);
            await db.SaveChangesAsync();
            await LoadAdministrationAsync();
        }
        catch (Exception ex) { ShowError(ex); }
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  LINE VIEW MODELS (simple classes for DataGrid editing)
// ═══════════════════════════════════════════════════════════════════════

public class ImportLine : INotifyPropertyChanged
{
    private Object? _product;
    public Object? Product { get => _product; set { _product = value; OnChanged(nameof(Product)); } }

    private int _count = 1;
    public int Count { get => _count; set { _count = value; OnChanged(nameof(Count)); } }

    private double _inputPrice;
    public double InputPrice { get => _inputPrice; set { _inputPrice = value; OnChanged(nameof(InputPrice)); } }

    private double _outputPrice;
    public double OutputPrice { get => _outputPrice; set { _outputPrice = value; OnChanged(nameof(OutputPrice)); } }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class ExportLine : INotifyPropertyChanged
{
    private Object? _product;
    public Object? Product { get => _product; set { _product = value; OnChanged(nameof(Product)); } }

    private int _count = 1;
    public int Count { get => _count; set { _count = value; OnChanged(nameof(Count)); } }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
