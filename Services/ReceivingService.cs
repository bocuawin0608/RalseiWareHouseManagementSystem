using System;
using System.Collections.Generic;
using System.Linq;
using RalseiWarehouse_v2.Data;
using RalseiWarehouse_v2.Models;
using RalseiWarehouse_v2.ViewState;

namespace RalseiWarehouse_v2.Services;

public class ReceivingService : IReceivingService
{
    public int CreateInboundOrder(string orderNo, int supplierId,
                                  DateTime? scheduledStart, DateTime? scheduledEnd,
                                  string? note, int createdBy, List<OrderLineInput> lines)
    {
        if (string.IsNullOrWhiteSpace(orderNo))
            throw new ArgumentException("Order number is required.", nameof(orderNo));
        if (lines == null || lines.Count == 0)
            throw new ArgumentException("Order must contain at least one line.", nameof(lines));

        using var db = new WarehouseDbContext();

        var receiving = db.Locations.First(l => l.LocationType == "Receiving");

        var header = new OrderHeader
        {
            OrderNo = orderNo,
            OrderType = WmsStatus.OrderType.In,
            AccountId = supplierId,
            Status = WmsStatus.Order.Confirmed,
            ScheduledStart = scheduledStart,
            ScheduledEnd = scheduledEnd,
            Note = note,
            CreatedBy = createdBy,
            CreatedAt = DateTime.Now
        };
        db.OrderHeaders.Add(header);

        foreach (var input in lines)
        {
            var line = new OrderLine
            {
                Order = header,
                ProductId = input.ProductId,
                UnitId = input.UnitId,
                QtyOrdered = input.Qty,
                UnitPrice = input.UnitPrice
            };
            db.OrderLines.Add(line);

            // Planner side of task assignment: receiving work waits in the queue
            db.WorkTasks.Add(new WorkTask
            {
                TaskType = WmsStatus.TaskType.Receive,
                Status = WmsStatus.Task.Open,
                OrderLine = line,
                ToLocationId = receiving.LocationId,
                Qty = input.Qty,
                CreatedAt = DateTime.Now
            });
        }

        db.SaveChanges();   // one transaction: header + lines + tasks
        return header.OrderId;
    }

    public void ExecuteReceive(long taskId, string? lotNumber, DateTime? expiryDate,
                               string? handlingUnit, int qty, int performedBy)
    {
        if (qty <= 0)
            throw new ArgumentOutOfRangeException(nameof(qty), "Receive qty must be positive.");

        using var db = new WarehouseDbContext();

        var task = db.WorkTasks.Find(taskId)
            ?? throw new InvalidOperationException("Task not found.");
        if (task.Status is WmsStatus.Task.Done or WmsStatus.Task.Cancelled)
            throw new InvalidOperationException("Task is already closed.");

        var line = db.OrderLines.Find(task.OrderLineId)
            ?? throw new InvalidOperationException("Order line not found.");

        // over-receipt guard: received-so-far comes from the audit ledger
        int receivedSoFar = db.StockMovements
            .Where(m => m.OrderLineId == line.OrderLineId && m.MovementType == WmsStatus.Move.Receipt)
            .Sum(m => (int?)m.Qty) ?? 0;
        if (receivedSoFar + qty > line.QtyOrdered)
            throw new InvalidOperationException(
                $"Over-receipt: ordered {line.QtyOrdered}, already received {receivedSoFar}.");

        // Stock row is born at the receiving dock (or topped up if identical)
        var stock = FindOrCreateStockRow(db, line.ProductId, task.ToLocationId!.Value,
            lotNumber, expiryDate, handlingUnit, WmsStatus.Quality.Available, line.OrderLineId);
        stock.QtyOnHand += qty;

        db.StockMovements.Add(new StockMovement
        {
            MovementDate = DateTime.Now,
            MovementType = WmsStatus.Move.Receipt,
            Stock = stock,
            ProductId = line.ProductId,
            Qty = qty,
            FromLocationId = null,              // came from outside
            ToLocationId = task.ToLocationId,
            OrderLineId = line.OrderLineId,
            TaskId = task.TaskId,
            PerformedBy = performedBy
        });

        // next job in the pipeline: put this qty away into storage
        db.WorkTasks.Add(new WorkTask
        {
            TaskType = WmsStatus.TaskType.Putaway,
            Status = WmsStatus.Task.Open,
            OrderLineId = line.OrderLineId,
            Stock = stock,
            FromLocationId = task.ToLocationId,
            Qty = qty,
            CreatedAt = DateTime.Now
        });

        BumpOrderToInProgress(db, line.OrderId);
        CloseTask(task, performedBy);
        db.SaveChanges();   // golden rule: stock + movement + task in ONE transaction
    }

    public void ExecutePutaway(long taskId, int toLocationId, int performedBy)
    {
        using var db = new WarehouseDbContext();

        var task = db.WorkTasks.Find(taskId)
            ?? throw new InvalidOperationException("Task not found.");
        if (task.Status is WmsStatus.Task.Done or WmsStatus.Task.Cancelled)
            throw new InvalidOperationException("Task is already closed.");

        var destination = db.Locations.Find(toLocationId)
            ?? throw new InvalidOperationException("Destination location not found.");
        if (destination.LocationType != "Storage")
            throw new InvalidOperationException("Putaway destination must be a Storage location.");

        var source = db.Stocks.Find(task.StockId)
            ?? throw new InvalidOperationException("Stock row not found.");

        int qty = task.Qty ?? source.QtyOnHand;
        if (qty <= 0 || qty > source.QtyOnHand)
            throw new InvalidOperationException($"Cannot put away {qty}; on hand is {source.QtyOnHand}.");

        var target = FindOrCreateStockRow(db, source.ProductId, toLocationId,
            source.LotNumber, source.ExpiryDate, source.HandlingUnit,
            source.QualityStatus, source.ReceiptLineId);
        target.QtyOnHand += qty;
        source.QtyOnHand -= qty;

        db.StockMovements.Add(new StockMovement
        {
            MovementDate = DateTime.Now,
            MovementType = WmsStatus.Move.Putaway,
            Stock = target,
            ProductId = source.ProductId,
            Qty = qty,
            FromLocationId = source.LocationId,
            ToLocationId = toLocationId,
            OrderLineId = task.OrderLineId,
            TaskId = task.TaskId,
            PerformedBy = performedBy
        });

        CloseTask(task, performedBy);
        db.SaveChanges();
    }

    // One stock row per (product, location, lot, expiry, pallet, quality, receipt)
    internal static Stock FindOrCreateStockRow(WarehouseDbContext db, int productId, int locationId,
        string? lotNumber, DateTime? expiryDate, string? handlingUnit,
        string qualityStatus, int? receiptLineId)
    {
        var row = db.Stocks.FirstOrDefault(s =>
            s.ProductId == productId && s.LocationId == locationId
            && s.LotNumber == lotNumber && s.ExpiryDate == expiryDate
            && s.HandlingUnit == handlingUnit && s.QualityStatus == qualityStatus
            && s.ReceiptLineId == receiptLineId && s.OwnerId == null);

        if (row == null)
        {
            row = new Stock
            {
                ProductId = productId,
                LocationId = locationId,
                LotNumber = lotNumber,
                ExpiryDate = expiryDate,
                HandlingUnit = handlingUnit,
                QualityStatus = qualityStatus,
                ReceiptLineId = receiptLineId,
                QtyOnHand = 0,
                QtyReserved = 0
            };
            db.Stocks.Add(row);
        }
        return row;
    }

    internal static void CloseTask(WorkTask task, int performedBy)
    {
        task.Status = WmsStatus.Task.Done;
        task.AssignedTo ??= performedBy;
        task.StartedAt ??= DateTime.Now;
        task.CompletedAt = DateTime.Now;
    }

    internal static void BumpOrderToInProgress(WarehouseDbContext db, int orderId)
    {
        var order = db.OrderHeaders.Find(orderId);
        if (order != null && order.Status == WmsStatus.Order.Confirmed)
            order.Status = WmsStatus.Order.InProgress;
    }
}
