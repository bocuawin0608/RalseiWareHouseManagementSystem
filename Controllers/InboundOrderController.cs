using System;
using System.Collections.Generic;
using System.Linq;
using RalseiWarehouse_v2.Data;
using RalseiWarehouse_v2.Models;
using RalseiWarehouse_v2.Services;
using RalseiWarehouse_v2.Session;
using RalseiWarehouse_v2.ViewState;

namespace RalseiWarehouse_v2.Controllers;

// Workflow A: register inbound orders and watch receipt progress.
public class InboundOrderController
{
    private readonly UserSession _session;
    private readonly IReceivingService _receivingService = new ReceivingService();

    public InboundOrderController(UserSession session) => _session = session;

    public List<LookupItem> GetSuppliers()
    {
        using var db = new WarehouseDbContext();
        var supplierRoleId = db.Roles.Where(r => r.Name == "Supplier").Select(r => r.RoleId).FirstOrDefault();
        return db.Accounts
            .Where(a => a.IsActive && a.RoleId == supplierRoleId)
            .Select(a => new LookupItem { Id = a.AccountId, DisplayName = a.DisplayName })
            .ToList();
    }

    public List<LookupItem> GetProducts()
    {
        using var db = new WarehouseDbContext();
        return db.Products
            .Where(p => p.IsActive)
            .Select(p => new LookupItem { Id = p.ProductId, DisplayName = p.SKU + " - " + p.DisplayName })
            .ToList();
    }

    public List<LookupItem> GetUnits()
    {
        using var db = new WarehouseDbContext();
        return db.Units
            .Select(u => new LookupItem { Id = u.UnitId, DisplayName = u.DisplayName })
            .ToList();
    }

    public List<OrderListItem> GetOrders()
    {
        using var db = new WarehouseDbContext();
        var partyNames = db.Accounts.ToDictionary(a => a.AccountId, a => a.DisplayName);
        var lineCounts = db.OrderLines
            .GroupBy(l => l.OrderId)
            .Select(g => new { OrderId = g.Key, Count = g.Count() })
            .ToDictionary(x => x.OrderId, x => x.Count);

        return db.OrderHeaders
            .Where(o => o.OrderType == WmsStatus.OrderType.In)
            .OrderByDescending(o => o.CreatedAt)
            .ToList()
            .Select(o => new OrderListItem
            {
                OrderId = o.OrderId,
                OrderNo = o.OrderNo,
                OrderType = o.OrderType,
                PartyName = partyNames.GetValueOrDefault(o.AccountId, "?"),
                Status = o.Status,
                ScheduledStart = o.ScheduledStart,
                ScheduledEnd = o.ScheduledEnd,
                LineCount = lineCounts.GetValueOrDefault(o.OrderId, 0),
                CreatedAt = o.CreatedAt
            })
            .ToList();
    }

    public List<OrderLineItem> GetOrderLines(int orderId)
    {
        using var db = new WarehouseDbContext();
        var productNames = db.Products.ToDictionary(p => p.ProductId, p => p.DisplayName);
        var unitNames = db.Units.ToDictionary(u => u.UnitId, u => u.DisplayName);
        var receivedByLineId = db.StockMovements
            .Where(m => m.MovementType == WmsStatus.Move.Receipt && m.OrderLineId != null)
            .GroupBy(m => m.OrderLineId!.Value)
            .Select(g => new { OrderLineId = g.Key, Qty = g.Sum(m => m.Qty) })
            .ToDictionary(x => x.OrderLineId, x => x.Qty);

        return db.OrderLines
            .Where(l => l.OrderId == orderId)
            .ToList()
            .Select(l => new OrderLineItem
            {
                OrderLineId = l.OrderLineId,
                ProductName = productNames.GetValueOrDefault(l.ProductId, "?"),
                UnitName = unitNames.GetValueOrDefault(l.UnitId, "?"),
                QtyOrdered = l.QtyOrdered,
                QtyAllocated = l.QtyAllocated,
                QtyPicked = l.QtyPicked,
                QtyShipped = l.QtyShipped,
                ReceivedQty = receivedByLineId.GetValueOrDefault(l.OrderLineId, 0),
                UnitPrice = l.UnitPrice
            })
            .ToList();
    }

    public int CreateOrder(string orderNo, int supplierId,
                           DateTime? scheduledStart, DateTime? scheduledEnd,
                           string? note, List<OrderLineInput> lines)
    {
        return _receivingService.CreateInboundOrder(
            orderNo, supplierId, scheduledStart, scheduledEnd, note, _session.AccountId, lines);
    }
}
