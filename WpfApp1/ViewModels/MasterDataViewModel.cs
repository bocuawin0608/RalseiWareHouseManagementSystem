using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RalseiWarehouse.Models;
using RalseiWarehouse.Services;
using Product = RalseiWarehouse.Models.Object;

namespace RalseiWarehouse.ViewModels;

/// <summary>Coordinates CRUD screens for units, suppliers, customers, and products.</summary>
public partial class MasterDataViewModel(IWarehouseService service) : ViewModelBase
{
    public ObservableCollection<Unit> Units { get; } = []; public ObservableCollection<Supplier> Suppliers { get; } = []; public ObservableCollection<Customer> Customers { get; } = []; public ObservableCollection<Product> Products { get; } = [];
    [ObservableProperty] private Unit? selectedUnit; [ObservableProperty] private Supplier? selectedSupplier; [ObservableProperty] private Customer? selectedCustomer; [ObservableProperty] private Product? selectedProduct;
    /// <summary>Loads every master-data lookup asynchronously.</summary>
    [RelayCommand] public async Task LoadAsync() => await ExecuteAsync(async () => { Replace(Units, await service.GetUnitsAsync()); Replace(Suppliers, await service.GetSuppliersAsync()); Replace(Customers, await service.GetCustomersAsync()); Replace(Products, await service.GetProductsAsync()); });
    /// <summary>Starts a new unit.</summary>
    [RelayCommand] private void NewUnit() { SelectedUnit = new(); Message = "Fill in the fields and click Save."; }
    /// <summary>Saves the selected unit.</summary>
    [RelayCommand] private Task SaveUnitAsync() => WriteAsync(() => service.SaveUnitAsync(SelectedUnit ?? throw Select()));
    /// <summary>Deletes the selected unit.</summary>
    [RelayCommand] private Task DeleteUnitAsync() => WriteAsync(() => service.DeleteUnitAsync((SelectedUnit ?? throw Select()).UnitId));
    /// <summary>Starts a new supplier.</summary>
    [RelayCommand] private void NewSupplier() { SelectedSupplier = new() { ContractDate = DateTime.Today }; Message = "Fill in the fields and click Save."; }
    /// <summary>Saves the selected supplier.</summary>
    [RelayCommand] private Task SaveSupplierAsync() => WriteAsync(() => service.SaveSupplierAsync(SelectedSupplier ?? throw Select()));
    /// <summary>Deletes the selected supplier.</summary>
    [RelayCommand] private Task DeleteSupplierAsync() => WriteAsync(() => service.DeleteSupplierAsync((SelectedSupplier ?? throw Select()).SupplierId));
    /// <summary>Starts a new customer.</summary>
    [RelayCommand] private void NewCustomer() { SelectedCustomer = new() { ContractDate = DateTime.Today }; Message = "Fill in the fields and click Save."; }
    /// <summary>Saves the selected customer.</summary>
    [RelayCommand] private Task SaveCustomerAsync() => WriteAsync(() => service.SaveCustomerAsync(SelectedCustomer ?? throw Select()));
    /// <summary>Deletes the selected customer.</summary>
    [RelayCommand] private Task DeleteCustomerAsync() => WriteAsync(() => service.DeleteCustomerAsync((SelectedCustomer ?? throw Select()).CustomerId));
    /// <summary>Starts a new product.</summary>
    [RelayCommand] private void NewProduct() { SelectedProduct = new(); Message = "Fill in the fields and click Save."; }
    /// <summary>Saves the selected product.</summary>
    [RelayCommand] private Task SaveProductAsync() => WriteAsync(() => service.SaveProductAsync(SelectedProduct ?? throw Select()));
    /// <summary>Deletes the selected product.</summary>
    [RelayCommand] private Task DeleteProductAsync() => WriteAsync(() => service.DeleteProductAsync((SelectedProduct ?? throw Select()).Id));
    /// <summary>Runs a write and refreshes all bound lists.</summary>
    private Task WriteAsync(Func<Task> operation) => ExecuteAsync(async () => { await operation(); Replace(Units, await service.GetUnitsAsync()); Replace(Suppliers, await service.GetSuppliersAsync()); Replace(Customers, await service.GetCustomersAsync()); Replace(Products, await service.GetProductsAsync()); Message = "Saved successfully."; });
    /// <summary>Creates a standard missing-selection error.</summary>
    private static InvalidOperationException Select() => new("Select a record first.");
}
