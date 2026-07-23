using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using RalseiWarehouse_v2.Data;
using RalseiWarehouse_v2.Models;
using RalseiWarehouse_v2.ViewState;

namespace RalseiWarehouse_v2.Services;

public class StockAllocationService : IStockAllocationService
{
    public int CreateOutboundOrder(string orderNo, int customerId,
                                   DateTime? scheduledStart, DateTime? scheduledEnd,
                                   string? note, int createdBy, List<OrderLineInput> lines)
    {
        if (string.IsNullOrWhiteSpace(orderNo))
            throw new ArgumentException("Order number is required.", nameof(orderNo));
        if (lines == null || lines.Count == 0)
            throw new ArgumentException("Order must contain at least one line.", nameof(lines));

        using var db = new WarehouseDbContext();

        var header = new OrderHeader
        {
            OrderNo = orderNo,
            OrderType = WmsStatus.OrderType.Out,
            AccountId = customerId,
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
            db.OrderLines.Add(new OrderLine
            {
                Order = header,
                ProductId = input.ProductId,
                UnitId = input.UnitId,
                QtyOrdered = input.Qty,
                UnitPrice = input.UnitPrice
            });
        }

        db.SaveChanges();
        return header.OrderId;
    }

    // Chapter 10: System.Text.Json against Account.CustomerRules
    public int GetMinShelfLifeDays(int customerId)
    {
        using var db = new WarehouseDbContext();
        var rules = db.Accounts.Find(customerId)?.CustomerRules;
        if (string.IsNullOrWhiteSpace(rules))
            return 0;

        using var doc = JsonDocument.Parse(rules);
        if (doc.RootElement.TryGetProperty("minShelfLifeDays", out var value)
            && value.TryGetInt32(out int days))
            return days;

        return 0;
    }

    public List<FefoCandidate> GetFefoCandidates(int productId, int minShelfLifeDays)
    {
        using var db = new WarehouseDbContext();
        return QueryFefo(db, productId, minShelfLifeDays);
    }

    public void AllocateOrder(int orderId, int performedBy)
    {
        using var db = new WarehouseDbContext();

        var order = db.OrderHeaders.Find(orderId)
            ?? throw new InvalidOperationException("Order not found.");
        if (order.OrderType != WmsStatus.OrderType.Out)
            throw new InvalidOperationException("Only outbound orders can be allocated.");
        if (order.Status is not (WmsStatus.Order.Confirmed or WmsStatus.Order.InProgress))
            throw new InvalidOperationException($"Cannot allocate an order in status {order.Status}.");

        int minShelfLifeDays = GetMinShelfLifeDays(order.AccountId);

        var lines = db.OrderLines
            .Where(l => l.OrderId == orderId)
            .ToList();

        foreach (var line in lines)
        {
            int needed = line.QtyOrdered - line.QtyAllocated;
            if (needed <= 0)
                continue;

            var candidates = QueryFefo(db, line.ProductId, minShelfLifeDays);
            foreach (var candidate in candidates)
            {
                if (needed == 0)
                    break;

                int take = Math.Min(needed, candidate.QtyAvailable);
                var stock = db.Stocks.Find(candidate.StockId)!;

                stock.QtyReserved += take;      // sticky note on the shelf
                line.QtyAllocated += take;
                needed -= take;

                db.StockReservations.Add(new StockReservation
                {
                    OrderLineId = line.OrderLineId,
                    StockId = stock.StockId,
                    Qty = take,
                    CreatedAt = DateTime.Now
                });

                // worker's next job: pick this qty from this location
                db.WorkTasks.Add(new WorkTask
                {
                    TaskType = WmsStatus.TaskType.Pick,
                    Status = WmsStatus.Task.Open,
                    OrderLineId = line.OrderLineId,
                    StockId = stock.StockId,
                    FromLocationId = stock.LocationId,
                    Qty = take,
                    CreatedAt = DateTime.Now
                });
            }

            if (needed > 0)
                throw new InvalidOperationException(
                    $"Not enough available stock for product #{line.ProductId}: short {needed} units. " +
                    "Nothing was saved.");
        }

        order.Status = WmsStatus.Order.InProgress;
        db.SaveChanges();   // all reservations + counters + pick tasks in one transaction
    }

    // FEFO = First Expired, First Out; stock without expiry is picked last.
    private static List<FefoCandidate> QueryFefo(WarehouseDbContext db, int productId, int minShelfLifeDays)
    {
        var cutoff = DateTime.Today.AddDays(minShelfLifeDays);

        return db.Stocks
            .Where(s => s.ProductId == productId
                     && s.QualityStatus == WmsStatus.Quality.Available
                     && s.QtyOnHand > 0
                     && s.QtyOnHand > s.QtyReserved
                     && (s.ExpiryDate == null || s.ExpiryDate >= cutoff))
            .OrderBy(s => s.ExpiryDate == null)   // rows WITH expiry first
            .ThenBy(s => s.ExpiryDate)            // earliest expiry first
            .ThenBy(s => s.StockId)
            .Select(s => new FefoCandidate
            {
                StockId = s.StockId,
                LocationId = s.LocationId,
                LotNumber = s.LotNumber,
                ExpiryDate = s.ExpiryDate,
                QtyAvailable = s.QtyOnHand - s.QtyReserved
            })
            .ToList();
    }
}
