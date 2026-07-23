using System;
using System.Linq;
using RalseiWarehouse_v2.Data;
using RalseiWarehouse_v2.Models;

namespace RalseiWarehouse_v2.Services;

public class ShippingService : IShippingService
{
    public void ExecutePick(long taskId, int performedBy)
    {
        using var db = new WarehouseDbContext();

        var task = db.WorkTasks.Find(taskId)
            ?? throw new InvalidOperationException("Task not found.");
        if (task.Status is WmsStatus.Task.Done or WmsStatus.Task.Cancelled)
            throw new InvalidOperationException("Task is already closed.");

        var line = db.OrderLines.Find(task.OrderLineId)
            ?? throw new InvalidOperationException("Order line not found.");
        var source = db.Stocks.Find(task.StockId)
            ?? throw new InvalidOperationException("Stock row not found.");

        int qty = task.Qty ?? 0;
        if (qty <= 0)
            throw new InvalidOperationException("Pick task has no qty.");

        // reservation consumed: sticky note comes off, goods physically move
        source.QtyOnHand -= qty;
        source.QtyReserved -= qty;
        if (source.QtyOnHand < 0 || source.QtyReserved < 0)
            throw new InvalidOperationException("Stock changed since allocation - re-allocate the order.");

        var reservation = db.StockReservations
            .FirstOrDefault(r => r.OrderLineId == line.OrderLineId && r.StockId == source.StockId);
        if (reservation != null)
        {
            reservation.Qty -= qty;
            if (reservation.Qty <= 0)
                db.StockReservations.Remove(reservation);
        }

        var staging = db.Locations.First(l => l.LocationType == "Staging");

        // staging row carries the same traceability, pallet id is assigned at pack time
        var staged = ReceivingService.FindOrCreateStockRow(db, source.ProductId, staging.LocationId,
            source.LotNumber, source.ExpiryDate, null, WmsStatus.Quality.Available, source.ReceiptLineId);
        staged.QtyOnHand += qty;

        db.StockMovements.Add(new StockMovement
        {
            MovementDate = DateTime.Now,
            MovementType = WmsStatus.Move.Pick,
            Stock = staged,
            ProductId = source.ProductId,
            Qty = qty,
            FromLocationId = source.LocationId,
            ToLocationId = staging.LocationId,
            OrderLineId = line.OrderLineId,
            TaskId = task.TaskId,
            PerformedBy = performedBy
        });

        line.QtyPicked += qty;

        db.WorkTasks.Add(new WorkTask
        {
            TaskType = WmsStatus.TaskType.Pack,
            Status = WmsStatus.Task.Open,
            OrderLineId = line.OrderLineId,
            Stock = staged,
            FromLocationId = staging.LocationId,
            ToLocationId = staging.LocationId,
            Qty = qty,
            CreatedAt = DateTime.Now
        });

        ReceivingService.BumpOrderToInProgress(db, line.OrderId);
        ReceivingService.CloseTask(task, performedBy);
        db.SaveChanges();
    }

    public void ExecutePack(long taskId, string? handlingUnit, int performedBy)
    {
        using var db = new WarehouseDbContext();

        var task = db.WorkTasks.Find(taskId)
            ?? throw new InvalidOperationException("Task not found.");
        if (task.Status is WmsStatus.Task.Done or WmsStatus.Task.Cancelled)
            throw new InvalidOperationException("Task is already closed.");

        var staged = db.Stocks.Find(task.StockId)
            ?? throw new InvalidOperationException("Staged stock not found.");

        // pallet/carton id (LPN); auto-generated when the worker leaves it empty
        staged.HandlingUnit = string.IsNullOrWhiteSpace(handlingUnit)
            ? $"PAL-{task.TaskId}"
            : handlingUnit;

        db.StockMovements.Add(new StockMovement
        {
            MovementDate = DateTime.Now,
            MovementType = WmsStatus.Move.Pack,
            Stock = staged,
            ProductId = staged.ProductId,
            Qty = task.Qty ?? staged.QtyOnHand,
            FromLocationId = task.FromLocationId,
            ToLocationId = task.ToLocationId,
            OrderLineId = task.OrderLineId,
            TaskId = task.TaskId,
            PerformedBy = performedBy
        });

        db.WorkTasks.Add(new WorkTask
        {
            TaskType = WmsStatus.TaskType.Load,
            Status = WmsStatus.Task.Open,
            OrderLineId = task.OrderLineId,
            Stock = staged,
            FromLocationId = task.ToLocationId,
            Qty = task.Qty,
            CreatedAt = DateTime.Now
        });

        ReceivingService.CloseTask(task, performedBy);
        db.SaveChanges();
    }

    public void ExecuteShip(long taskId, int performedBy)
    {
        using var db = new WarehouseDbContext();

        var task = db.WorkTasks.Find(taskId)
            ?? throw new InvalidOperationException("Task not found.");
        if (task.Status is WmsStatus.Task.Done or WmsStatus.Task.Cancelled)
            throw new InvalidOperationException("Task is already closed.");

        var line = db.OrderLines.Find(task.OrderLineId)
            ?? throw new InvalidOperationException("Order line not found.");
        var staged = db.Stocks.Find(task.StockId)
            ?? throw new InvalidOperationException("Staged stock not found.");

        int qty = task.Qty ?? staged.QtyOnHand;
        staged.QtyOnHand -= qty;
        if (staged.QtyOnHand < 0)
            throw new InvalidOperationException("Staged qty is no longer available.");

        db.StockMovements.Add(new StockMovement
        {
            MovementDate = DateTime.Now,
            MovementType = WmsStatus.Move.Ship,
            Stock = staged,
            ProductId = staged.ProductId,
            Qty = -qty,                         // signed: leaving the warehouse
            FromLocationId = task.FromLocationId,
            ToLocationId = null,                // gone to the customer
            OrderLineId = line.OrderLineId,
            TaskId = task.TaskId,
            PerformedBy = performedBy
        });

        line.QtyShipped += qty;

        // dispatch confirmation: order completes when every line is fully shipped
        var orderLines = db.OrderLines.Where(l => l.OrderId == line.OrderId).ToList();
        var order = db.OrderHeaders.Find(line.OrderId)!;
        bool allShipped = orderLines.All(l => l.QtyShipped >= l.QtyOrdered);
        order.Status = allShipped ? WmsStatus.Order.Completed : WmsStatus.Order.InProgress;

        ReceivingService.CloseTask(task, performedBy);
        db.SaveChanges();
    }
}
