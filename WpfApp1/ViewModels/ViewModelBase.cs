using CommunityToolkit.Mvvm.ComponentModel;

namespace RalseiWarehouse.ViewModels;

/// <summary>Provides consistent asynchronous error reporting for screen view models.</summary>
public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty] private string message = string.Empty;
    [ObservableProperty] private bool isBusy;

    /// <summary>Executes UI work once and exposes a safe user-facing error.</summary>
    /// <param name="operation">The asynchronous operation.</param>
    protected async Task ExecuteAsync(Func<Task> operation)
    {
        if (IsBusy) return;
        try { IsBusy = true; Message = string.Empty; await operation(); }
        catch (Exception exception) { Message = exception.Message; }
        finally { IsBusy = false; }
    }

    /// <summary>Replaces an observable collection without breaking its binding.</summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="target">The bound collection.</param>
    /// <param name="items">The replacement values.</param>
    protected static void Replace<T>(System.Collections.ObjectModel.ObservableCollection<T> target, IEnumerable<T> items) { target.Clear(); foreach (var item in items) target.Add(item); }
}
