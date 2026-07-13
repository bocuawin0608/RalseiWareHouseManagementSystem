using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RalseiWarehouse.Models;
using RalseiWarehouse.Services;
using Product = RalseiWarehouse.Models.Object;

namespace RalseiWarehouse.ViewModels;

/// <summary>Coordinates atomic import and export creation.</summary>
public partial class TransactionViewModel(IWarehouseService service) : ViewModelBase
{
    public ObservableCollection<Product> Products { get; } = []; public ObservableCollection<Customer> Customers { get; } = []; public ObservableCollection<InputLineViewModel> InputLines { get; } = []; public ObservableCollection<OutputLineViewModel> OutputLines { get; } = [];
    [ObservableProperty] private Customer? selectedCustomer;
    /// <summary>Loads product and customer choices.</summary>
    [RelayCommand] public async Task LoadAsync() => await ExecuteAsync(async () => { Replace(Products, await service.GetProductsAsync()); Replace(Customers, await service.GetCustomersAsync()); });
    /// <summary>Adds an import line editor.</summary>
    [RelayCommand] private void AddInput() => InputLines.Add(new());
    /// <summary>Removes an import line editor.</summary>
    [RelayCommand] private void RemoveInput(InputLineViewModel? line) { if (line is not null) InputLines.Remove(line); }
    /// <summary>Commits one import receipt.</summary>
    [RelayCommand] private Task SaveInputAsync() => ExecuteAsync(async () => { await service.CreateInputAsync(InputLines.Select(x => new InputRequest(x.Product?.Id ?? "", x.Count, x.InputPrice, x.OutputPrice))); InputLines.Clear(); Message = "Import completed."; });
    /// <summary>Adds an export line editor.</summary>
    [RelayCommand] private void AddOutput() => OutputLines.Add(new());
    /// <summary>Removes an export line editor.</summary>
    [RelayCommand] private void RemoveOutput(OutputLineViewModel? line) { if (line is not null) OutputLines.Remove(line); }
    /// <summary>Validates stock and commits one export receipt.</summary>
    [RelayCommand] private Task SaveOutputAsync() => ExecuteAsync(async () => { await service.CreateOutputAsync(SelectedCustomer?.CustomerId ?? 0, OutputLines.Select(x => new OutputRequest(x.Product?.Id ?? "", x.Count))); OutputLines.Clear(); Message = "Export completed."; });
}

/// <summary>An editable import line.</summary>
public partial class InputLineViewModel : ObservableObject { [ObservableProperty] private Product? product; [ObservableProperty] private int count = 1; [ObservableProperty] private double inputPrice; [ObservableProperty] private double outputPrice; }
/// <summary>An editable export line.</summary>
public partial class OutputLineViewModel : ObservableObject { [ObservableProperty] private Product? product; [ObservableProperty] private int count = 1; }
