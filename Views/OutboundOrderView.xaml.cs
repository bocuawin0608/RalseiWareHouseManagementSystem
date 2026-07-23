using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using RalseiWarehouse_v2.Controllers;
using RalseiWarehouse_v2.Session;
using RalseiWarehouse_v2.ViewState;

namespace RalseiWarehouse_v2.Views;

public partial class OutboundOrderView : UserControl
{
    private readonly OutboundOrderController _controller;
    private readonly List<OrderLineInput> _pendingLines = new();

    public OutboundOrderView(UserSession session)
    {
        InitializeComponent();
        _controller = new OutboundOrderController(session);

        cboCustomer.ItemsSource = _controller.GetCustomers();
        cboProduct.ItemsSource = _controller.GetProducts();
        cboUnit.ItemsSource = _controller.GetUnits();
        RefreshOrders();
    }

    private void btnAddLine_Click(object sender, RoutedEventArgs e)
    {
        if (cboProduct.SelectedValue is not int productId)
        {
            MessageBox.Show("Select a product.");
            return;
        }
        if (cboUnit.SelectedValue is not int unitId)
        {
            MessageBox.Show("Select a unit.");
            return;
        }
        if (!int.TryParse(txtQty.Text, out int qty) || qty <= 0)
        {
            MessageBox.Show("Qty must be a positive number.");
            return;
        }
        decimal? price = decimal.TryParse(txtPrice.Text, out decimal p) ? p : null;

        _pendingLines.Add(new OrderLineInput
        {
            ProductId = productId,
            ProductName = cboProduct.Text,
            UnitId = unitId,
            UnitName = cboUnit.Text,
            Qty = qty,
            UnitPrice = price
        });
        dgPending.ItemsSource = _pendingLines.ToList();
        txtQty.Clear();
        txtPrice.Clear();
    }

    private void btnCreate_Click(object sender, RoutedEventArgs e)
    {
        if (cboCustomer.SelectedValue is not int customerId)
        {
            MessageBox.Show("Select a customer.");
            return;
        }

        try
        {
            int orderId = _controller.CreateOrder(
                txtOrderNo.Text.Trim(), customerId,
                dtpStart.SelectedDate, dtpEnd.SelectedDate,
                string.IsNullOrWhiteSpace(txtNote.Text) ? null : txtNote.Text.Trim(),
                _pendingLines);

            MessageBox.Show($"Outbound order #{orderId} created. Select it and press Allocate (FEFO).");
            _pendingLines.Clear();
            dgPending.ItemsSource = null;
            txtOrderNo.Clear();
            txtNote.Clear();
            RefreshOrders();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Cannot create order", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void btnAllocate_Click(object sender, RoutedEventArgs e)
    {
        if (dgOrders.SelectedItem is not OrderListItem order)
        {
            MessageBox.Show("Select an order first.");
            return;
        }

        try
        {
            _controller.Allocate(order.OrderId);
            MessageBox.Show("Order allocated. Pick tasks are waiting in the Work Queue.");
            RefreshOrders();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Allocation failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void btnRefresh_Click(object sender, RoutedEventArgs e) => RefreshOrders();

    private void dgOrders_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (dgOrders.SelectedItem is OrderListItem order)
            dgLines.ItemsSource = _controller.GetOrderLines(order.OrderId);
    }

    private void RefreshOrders()
    {
        dgOrders.ItemsSource = _controller.GetOrders();
        dgLines.ItemsSource = null;
    }
}
