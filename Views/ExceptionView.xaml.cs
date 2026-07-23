using System;
using System.Windows;
using System.Windows.Controls;
using RalseiWarehouse_v2.Controllers;
using RalseiWarehouse_v2.Session;
using RalseiWarehouse_v2.ViewState;

namespace RalseiWarehouse_v2.Views;

public partial class ExceptionView : UserControl
{
    private readonly ExceptionController _controller;

    public ExceptionView(UserSession session)
    {
        InitializeComponent();
        _controller = new ExceptionController(session);

        cboDestination.ItemsSource = _controller.GetStorageLocations();
        RefreshStock();
    }

    private void btnRefresh_Click(object sender, RoutedEventArgs e) => RefreshStock();

    private void cboQuality_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded)
            RefreshStock();
    }

    private void btnDamage_Click(object sender, RoutedEventArgs e)
        => RunQtyAction((row, qty, note) => _controller.ReportDamage(row.StockId, qty, note));

    private void btnQuarantine_Click(object sender, RoutedEventArgs e)
        => RunQtyAction((row, qty, note) => _controller.Quarantine(row.StockId, qty, note));

    private void btnCountAdjust_Click(object sender, RoutedEventArgs e)
        => RunQtyAction((row, qty, note) => _controller.CountAdjust(row.StockId, qty, note));

    private void btnRelease_Click(object sender, RoutedEventArgs e)
    {
        if (dgStock.SelectedItem is not StockRowDisplay row)
        {
            MessageBox.Show("Select a stock row first.");
            return;
        }
        if (cboDestination.SelectedValue is not int destinationId)
        {
            MessageBox.Show("Choose a storage location to release to.");
            return;
        }

        try
        {
            _controller.Release(row.StockId, destinationId);
            MessageBox.Show("Stock released back to Available.");
            RefreshStock();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RunQtyAction(Action<StockRowDisplay, int, string> action)
    {
        if (dgStock.SelectedItem is not StockRowDisplay row)
        {
            MessageBox.Show("Select a stock row first.");
            return;
        }
        if (!int.TryParse(txtQty.Text, out int qty) || qty < 0)
        {
            MessageBox.Show("Enter a valid qty (counted qty for count adjust).");
            return;
        }

        try
        {
            action(row, qty, txtNote.Text.Trim());
            MessageBox.Show("Done. Evidence written to the movement ledger.");
            txtQty.Clear();
            txtNote.Clear();
            RefreshStock();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RefreshStock()
    {
        string filter = (cboQuality.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All";
        dgStock.ItemsSource = _controller.GetStockRows(filter);
    }
}
