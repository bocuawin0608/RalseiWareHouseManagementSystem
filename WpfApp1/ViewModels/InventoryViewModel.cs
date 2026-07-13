using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RalseiWarehouse.Services;

namespace RalseiWarehouse.ViewModels;

/// <summary>Coordinates the derived current-inventory report.</summary>
public partial class InventoryViewModel(IWarehouseService service) : ViewModelBase
{
    public ObservableCollection<InventoryRow> Rows { get; } = []; [ObservableProperty] private string search = string.Empty;
    /// <summary>Loads the filtered inventory report.</summary>
    [RelayCommand] public async Task LoadAsync() => await ExecuteAsync(async () => Replace(Rows, await service.GetInventoryAsync(Search)));
}
