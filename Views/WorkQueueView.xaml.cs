using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using RalseiWarehouse_v2.Controllers;
using RalseiWarehouse_v2.Models;
using RalseiWarehouse_v2.Session;
using RalseiWarehouse_v2.ViewState;

namespace RalseiWarehouse_v2.Views;

public partial class WorkQueueView : UserControl
{
    private readonly WorkQueueController _controller;

    public WorkQueueView(UserSession session)
    {
        InitializeComponent();
        _controller = new WorkQueueController(session);

        cboDestination.ItemsSource = _controller.GetStorageLocations();

        if (session.RoleName == "Admin")
        {
            panelAdminAssign.Visibility = Visibility.Visible;
            cboWorker.ItemsSource = _controller.GetWorkers();
            if (cboWorker.Items.Cast<LookupItem>().Any())
                cboWorker.SelectedIndex = 0;
        }

        RefreshTasks();
    }

    private void btnRefresh_Click(object sender, RoutedEventArgs e) => RefreshTasks();

    private void btnAssign_Click(object sender, RoutedEventArgs e)
        => RunOnSelected(taskId => _controller.AssignToMe(taskId));

    private void btnStart_Click(object sender, RoutedEventArgs e)
        => RunOnSelected(taskId => _controller.StartTask(taskId));

    private void btnCancel_Click(object sender, RoutedEventArgs e)
        => RunOnSelected(taskId => _controller.CancelTask(taskId));

    private void btnAssignToWorker_Click(object sender, RoutedEventArgs e)
    {
        if (cboWorker.SelectedValue is not int workerId)
        {
            MessageBox.Show("Select a worker first.");
            return;
        }
        RunOnSelected(taskId => _controller.AssignToWorker(taskId, workerId));
    }

    private void dgTasks_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // pre-fill qty from the task so the worker just confirms it
        if (dgTasks.SelectedItem is WorkTaskItem task)
        {
            // qty is embedded in the Info text; leave the box for the worker to type/confirm
            txtQty.Clear();
        }
    }

    private void btnExecute_Click(object sender, RoutedEventArgs e)
    {
        if (dgTasks.SelectedItem is not WorkTaskItem task)
        {
            MessageBox.Show("Select a task first.");
            return;
        }

        try
        {
            switch (task.TaskType)
            {
                case WmsStatus.TaskType.Receive:
                    int qty = ReadQty();
                    _controller.ExecuteReceive(task.TaskId,
                        EmptyToNull(txtLot.Text), dtpExpiry.SelectedDate, EmptyToNull(txtHandlingUnit.Text), qty);
                    break;

                case WmsStatus.TaskType.Putaway:
                    if (cboDestination.SelectedValue is not int destinationId)
                    {
                        MessageBox.Show("Choose a destination storage location.");
                        return;
                    }
                    _controller.ExecutePutaway(task.TaskId, destinationId);
                    break;

                case WmsStatus.TaskType.Pick:
                    _controller.ExecutePick(task.TaskId);
                    break;

                case WmsStatus.TaskType.Pack:
                    _controller.ExecutePack(task.TaskId, EmptyToNull(txtHandlingUnit.Text));
                    break;

                case WmsStatus.TaskType.Load:
                    _controller.ExecuteShip(task.TaskId);
                    break;

                default:
                    MessageBox.Show($"Don't know how to execute a '{task.TaskType}' task.");
                    return;
            }

            MessageBox.Show("Task completed.");
            ClearInputs();
            RefreshTasks();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Cannot execute task", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private int ReadQty()
    {
        if (!int.TryParse(txtQty.Text, out int qty) || qty <= 0)
            throw new ArgumentException("Enter a valid positive qty.");
        return qty;
    }

    private void RunOnSelected(Action<long> action)
    {
        if (dgTasks.SelectedItem is not WorkTaskItem task)
        {
            MessageBox.Show("Select a task first.");
            return;
        }

        try
        {
            action(task.TaskId);
            RefreshTasks();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RefreshTasks()
    {
        var (tasks, totalInDb, openCount) = _controller.GetOpenTasks();
        dgTasks.ItemsSource = tasks;
        txtCount.Text = $"({tasks.Count} shown · {openCount} open · {totalInDb} total in DB)";
    }

    private void ClearInputs()
    {
        txtQty.Clear();
        txtLot.Clear();
        txtHandlingUnit.Clear();
        dtpExpiry.SelectedDate = null;
    }

    private static string? EmptyToNull(string text)
        => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
