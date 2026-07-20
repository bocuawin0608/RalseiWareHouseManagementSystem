using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using RalseiWarehouse.Models;
using RalseiWarehouse.Services;
using Product = RalseiWarehouse.Models.Object;

namespace RalseiWarehouse.Views;

/// <summary>
/// Main application window with tabs for master data, transactions, inventory, dashboard, and administration.
/// </summary>
public partial class MainWindow : Window
{
    private readonly WarehouseService _service;
    private readonly User _currentUser;

    /// <summary>Gets whether the user chose to log out.</summary>
    public bool LoggedOut { get; private set; }

    /// <summary>Gets the product list for DataGrid ComboBox binding.</summary>
    public ObservableCollection<Product> Products { get; } = new();

    private readonly ObservableCollection<ImportLine> _importLines = new();
    private readonly ObservableCollection<ExportLine> _exportLines = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    /// <param name="user">The currently authenticated user.</param>
    public MainWindow(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        InitializeComponent();
        _service = WarehouseService.GetInstance(string.Empty);
        _currentUser = user;

        txtUserName.Text = $"Welcome, {user.DisplayName}";

        var role = user.Role?.DisplayName ?? "";
        tabAdmin.Visibility = role.Contains("Admin", StringComparison.OrdinalIgnoreCase)
            ? Visibility.Visible : Visibility.Collapsed;

        dgImportLines.ItemsSource = _importLines;
        dgExportLines.ItemsSource = _exportLines;

        Loaded += async (_, _) =>
        {
            try
            {
                await LoadMasterDataAsync();
                await LoadTransactionDataAsync();
            }
            catch (Exception ex) { ShowError(ex); }
        };
    }

    private void ShowError(Exception ex) =>
        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);

    // ── Tab Switch ──
    private async void TabMain_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.Source is TabControl tc && tc.SelectedItem is TabItem ti)
        {
            if (ti == tabInventory)
                await LoadInventoryAsync();
            else if (ti == tabDashboard)
                await LoadDashboardAsync();
            else if (ti == tabAdmin)
                await LoadAdminDataAsync();
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  MASTER DATA
    // ═══════════════════════════════════════════════════════════════════

    private async Task LoadMasterDataAsync()
    {
        try
        {
            dgUnits.ItemsSource = await _service.GetUnitsAsync();
            dgSuppliers.ItemsSource = await _service.GetSuppliersAsync();
            dgCustomers.ItemsSource = await _service.GetCustomersAsync();
            var products = await _service.GetProductsAsync();
            dgProducts.ItemsSource = products;

            Products.Clear();
            foreach (var p in products) Products.Add(p);

            cboProductUnit.ItemsSource = await _service.GetUnitsAsync();
            cboProductSupplier.ItemsSource = await _service.GetSuppliersAsync();
        }
        catch (Exception ex) { ShowError(ex); }
    }

    // ── Units ──
    private Unit? _selectedUnit;
    private void DgUnits_SelectionChanged(object sender, SelectionChangedEventArgs e)
    { _selectedUnit = dgUnits.SelectedItem as Unit; txtUnitName.Text = _selectedUnit?.DisplayName ?? ""; }
    private void BtnNewUnit_Click(object sender, RoutedEventArgs e)
    { _selectedUnit = null; txtUnitName.Text = ""; }
    private async void BtnSaveUnit_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(txtUnitName.Text)) { MessageBox.Show("Unit name is required."); return; }
            var unit = _selectedUnit ?? new Unit();
            unit.DisplayName = txtUnitName.Text.Trim();
            await _service.SaveUnitAsync(unit);
            await LoadMasterDataAsync();
        }
        catch (Exception ex) { ShowError(ex); }
    }
    private async void BtnDeleteUnit_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_selectedUnit is null || _selectedUnit.UnitId == 0) { MessageBox.Show("Select a unit first."); return; }
            await _service.DeleteUnitAsync(_selectedUnit.UnitId);
            await LoadMasterDataAsync();
        }
        catch (Exception ex) { ShowError(ex); }
    }

    // ── Suppliers ──
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
        _selectedSupplier = null;
        txtSupplierName.Text = txtSupplierAddr.Text = txtSupplierPhone.Text = txtSupplierEmail.Text = txtSupplierNotes.Text = "";
        dpSupplierDate.SelectedDate = DateTime.Today;
    }
    private async void BtnSaveSupplier_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(txtSupplierName.Text)) { MessageBox.Show("Supplier name is required."); return; }
            var s = _selectedSupplier ?? new Supplier();
            s.DisplayName = txtSupplierName.Text.Trim();
            s.Address = txtSupplierAddr.Text;
            s.Phone = txtSupplierPhone.Text;
            s.Email = txtSupplierEmail.Text;
            s.MoreInfo = txtSupplierNotes.Text;
            s.ContractDate = dpSupplierDate.SelectedDate;
            await _service.SaveSupplierAsync(s);
            await LoadMasterDataAsync();
        }
        catch (Exception ex) { ShowError(ex); }
    }
    private async void BtnDeleteSupplier_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_selectedSupplier is null || _selectedSupplier.SupplierId == 0) { MessageBox.Show("Select a supplier first."); return; }
            await _service.DeleteSupplierAsync(_selectedSupplier.SupplierId);
            await LoadMasterDataAsync();
        }
        catch (Exception ex) { ShowError(ex); }
    }

    // ── Customers ──
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
        _selectedCustomer = null;
        txtCustomerName.Text = txtCustomerAddr.Text = txtCustomerPhone.Text = txtCustomerEmail.Text = txtCustomerNotes.Text = "";
        dpCustomerDate.SelectedDate = DateTime.Today;
    }
    private async void BtnSaveCustomer_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(txtCustomerName.Text)) { MessageBox.Show("Customer name is required."); return; }
            var c = _selectedCustomer ?? new Customer();
            c.DisplayName = txtCustomerName.Text.Trim();
            c.Address = txtCustomerAddr.Text;
            c.Phone = txtCustomerPhone.Text;
            c.Email = txtCustomerEmail.Text;
            c.MoreInfo = txtCustomerNotes.Text;
            c.ContractDate = dpCustomerDate.SelectedDate;
            await _service.SaveCustomerAsync(c);
            await LoadMasterDataAsync();
        }
        catch (Exception ex) { ShowError(ex); }
    }
    private async void BtnDeleteCustomer_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_selectedCustomer is null || _selectedCustomer.CustomerId == 0) { MessageBox.Show("Select a customer first."); return; }
            await _service.DeleteCustomerAsync(_selectedCustomer.CustomerId);
            await LoadMasterDataAsync();
        }
        catch (Exception ex) { ShowError(ex); }
    }

    // ── Products ──
    private Product? _selectedProduct;
    private void DgProducts_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedProduct = dgProducts.SelectedItem as Product;
        if (_selectedProduct is null) return;
        txtProductName.Text = _selectedProduct.DisplayName ?? "";
        cboProductUnit.SelectedValue = _selectedProduct.UnitId;
        cboProductSupplier.SelectedValue = _selectedProduct.SupplierId;
        txtProductQR.Text = _selectedProduct.QRCode ?? "";
        txtProductBarcode.Text = _selectedProduct.BarCode ?? "";
    }
    private void BtnNewProduct_Click(object sender, RoutedEventArgs e)
    {
        _selectedProduct = null;
        txtProductName.Text = txtProductQR.Text = txtProductBarcode.Text = "";
        cboProductUnit.SelectedIndex = cboProductSupplier.SelectedIndex = -1;
    }
    private async void BtnSaveProduct_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var name = txtProductName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name)) { MessageBox.Show("Product name is required."); return; }
            if (cboProductUnit.SelectedValue is not int uid || uid < 1) { MessageBox.Show("Select a unit."); return; }
            if (cboProductSupplier.SelectedValue is not int sid || sid < 1) { MessageBox.Show("Select a supplier."); return; }

            var p = _selectedProduct ?? new Product();
            p.DisplayName = name;
            p.UnitId = uid;
            p.SupplierId = sid;
            p.QRCode = txtProductQR.Text;
            p.BarCode = txtProductBarcode.Text;
            await _service.SaveProductAsync(p);
            await LoadMasterDataAsync();
        }
        catch (Exception ex) { ShowError(ex); }
    }
    private async void BtnDeleteProduct_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_selectedProduct is null) { MessageBox.Show("Select a product first."); return; }
            await _service.DeleteProductAsync(_selectedProduct.Id);
            BtnNewProduct_Click(sender, e);
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
            var customers = await _service.GetCustomersAsync();
            cboExportCustomer.ItemsSource = customers;

            var imports = await _service.GetRecentImportsAsync();
            var exports = await _service.GetRecentExportsAsync();
            dgRecentImports.ItemsSource = imports;
            dgRecentExports.ItemsSource = exports;
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void BtnAddImportLine_Click(object sender, RoutedEventArgs e) => _importLines.Add(new ImportLine());
    private void BtnRemoveImportLine_Click(object sender, RoutedEventArgs e)
    { if (sender is Button btn && btn.Tag is ImportLine line) _importLines.Remove(line); }

    private async void BtnCompleteImport_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await _service.CreateImportAsync(_importLines.ToList());
            _importLines.Clear();
            txtImportMessage.Text = "Import saved successfully.";
            await LoadTransactionDataAsync();
            await LoadMasterDataAsync();
        }
        catch (Exception ex) { txtImportMessage.Text = ex.Message; txtImportMessage.Foreground = System.Windows.Media.Brushes.Red; }
    }

    private void BtnAddExportLine_Click(object sender, RoutedEventArgs e) => _exportLines.Add(new ExportLine());
    private void BtnRemoveExportLine_Click(object sender, RoutedEventArgs e)
    { if (sender is Button btn && btn.Tag is ExportLine line) _exportLines.Remove(line); }

    private async void BtnCompleteExport_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (cboExportCustomer.SelectedValue is not int cid || cid < 1)
            { MessageBox.Show("Select a customer."); return; }

            await _service.CreateExportAsync(cid, _exportLines.ToList());
            _exportLines.Clear();
            txtExportMessage.Text = "Export saved successfully.";
            await LoadTransactionDataAsync();
            await LoadMasterDataAsync();
        }
        catch (Exception ex) { txtExportMessage.Text = ex.Message; txtExportMessage.Foreground = System.Windows.Media.Brushes.Red; }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  INVENTORY
    // ═══════════════════════════════════════════════════════════════════

    private async Task LoadInventoryAsync()
    {
        try
        {
            dgInventory.ItemsSource = await _service.GetInventoryReportAsync(txtInventorySearch.Text.Trim());
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private async void BtnInventoryRefresh_Click(object sender, RoutedEventArgs e) => await LoadInventoryAsync();
    private async void TxtInventorySearch_TextChanged(object sender, TextChangedEventArgs e) => await LoadInventoryAsync();

    // ═══════════════════════════════════════════════════════════════════
    //  DASHBOARD + EXPORT/IMPORT
    // ═══════════════════════════════════════════════════════════════════

    private async Task LoadDashboardAsync()
    {
        try
        {
            var stats = await _service.GetDashboardStatsAsync();
            var lines = new System.Text.StringBuilder();
            foreach (var kv in stats)
                lines.AppendLine($"{kv.Key,-30}: {kv.Value}");
            txtDashboard.Text = lines.ToString();
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private async void BtnDashboardRefresh_Click(object sender, RoutedEventArgs e) => await LoadDashboardAsync();

    private async void BtnExportCsv_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            { Filter = "CSV files (*.csv)|*.csv", FileName = "warehouse_export.csv" };
            if (dlg.ShowDialog() == true)
            {
                var path = await _service.ExportToCsvAsync(dlg.FileName);
                txtDashboardMessage.Text = $"CSV exported to: {path}";
            }
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private async void BtnExportReport_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            { Filter = "Text files (*.txt)|*.txt", FileName = "warehouse_report.txt" };
            if (dlg.ShowDialog() == true)
            {
                var path = await _service.ExportReportToFileAsync(dlg.FileName);
                txtDashboardMessage.Text = $"Report exported to: {path}";
            }
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private async void BtnExportJson_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            { Filter = "JSON files (*.json)|*.json", FileName = "warehouse_data.json" };
            if (dlg.ShowDialog() == true)
            {
                var path = await _service.ExportToJsonAsync(dlg.FileName);
                txtDashboardMessage.Text = $"JSON exported to: {path}";
            }
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private async void BtnImportJson_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            { Filter = "JSON files (*.json)|*.json" };
            if (dlg.ShowDialog() == true)
            {
                var count = await _service.ImportFromJsonAsync(dlg.FileName);
                txtDashboardMessage.Text = $"Imported {count} products from JSON.";
                await LoadMasterDataAsync();
            }
        }
        catch (Exception ex) { ShowError(ex); }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  ADMINISTRATION
    // ═══════════════════════════════════════════════════════════════════

    private async Task LoadAdminDataAsync()
    {
        try
        {
            dgRoles.ItemsSource = await _service.GetRolesAsync();
            dgUsers.ItemsSource = await _service.GetUsersAsync();
            cboUserRole.ItemsSource = await _service.GetRolesAsync();
        }
        catch (Exception ex) { ShowError(ex); }
    }

    // ── Roles ──
    private Role? _selectedRole;
    private void DgRoles_SelectionChanged(object sender, SelectionChangedEventArgs e)
    { _selectedRole = dgRoles.SelectedItem as Role; txtRoleName.Text = _selectedRole?.DisplayName ?? ""; }
    private void BtnNewRole_Click(object sender, RoutedEventArgs e)
    { _selectedRole = null; txtRoleName.Text = ""; }
    private async void BtnSaveRole_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(txtRoleName.Text)) { MessageBox.Show("Role name is required."); return; }
            var r = _selectedRole ?? new Role();
            r.DisplayName = txtRoleName.Text.Trim();
            await _service.SaveRoleAsync(r);
            await LoadAdminDataAsync();
        }
        catch (Exception ex) { ShowError(ex); }
    }
    private async void BtnDeleteRole_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_selectedRole is null || _selectedRole.RoleId == 0) { MessageBox.Show("Select a role first."); return; }
            await _service.DeleteRoleAsync(_selectedRole.RoleId);
            await LoadAdminDataAsync();
        }
        catch (Exception ex) { ShowError(ex); }
    }

    // ── Users ──
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
        _selectedUser = null;
        txtUserDisplayName.Text = txtUserUsername.Text = txtUserPassword.Text = "";
        cboUserRole.SelectedIndex = -1;
    }
    private async void BtnSaveUser_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dn = txtUserDisplayName.Text.Trim();
            var un = txtUserUsername.Text.Trim();
            if (string.IsNullOrWhiteSpace(dn)) { MessageBox.Show("Display name is required."); return; }
            if (string.IsNullOrWhiteSpace(un)) { MessageBox.Show("Username is required."); return; }
            if (cboUserRole.SelectedValue is not int rid || rid < 1) { MessageBox.Show("Select a role."); return; }

            bool isNew = _selectedUser is null;
            if (isNew && string.IsNullOrWhiteSpace(txtUserPassword.Text)) { MessageBox.Show("Password is required for new users."); return; }

            var u = _selectedUser ?? new User();
            u.DisplayName = dn;
            u.UserName = un;
            u.Password = txtUserPassword.Text;
            u.RoleId = rid;
            await _service.SaveUserAsync(u, isNew);
            await LoadAdminDataAsync();
        }
        catch (Exception ex) { ShowError(ex); }
    }
    private async void BtnDeleteUser_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_selectedUser is null || _selectedUser.UserId == 0) { MessageBox.Show("Select a user first."); return; }
            if (_selectedUser.UserId == _currentUser.UserId) { MessageBox.Show("Cannot delete yourself."); return; }
            await _service.DeleteUserAsync(_selectedUser.UserId);
            BtnNewUser_Click(sender, e);
            await LoadAdminDataAsync();
        }
        catch (Exception ex) { ShowError(ex); }
    }

    // ── Logout ──
    private void BtnLogout_Click(object sender, RoutedEventArgs e)
    {
        LoggedOut = true;
        Close();
    }
}
