using System;
using System.Collections.Generic;
using System.Linq;
using RalseiWarehouse_v2.Data;
using RalseiWarehouse_v2.Models;
using RalseiWarehouse_v2.Services;
using RalseiWarehouse_v2.Session;
using RalseiWarehouse_v2.ViewState;

namespace RalseiWarehouse_v2.Controllers;

// Workflow B: register outbound orders, allocate FEFO, watch ship progress.
public class OutboundOrderController
{
    private readonly UserSession _session;
    private readonly IStockAllocationService _allocationService = new StockAllocationService();

    public OutboundOrderController(UserSession session) => _session = session;

    public List<LookupItem> GetCustomers()
    {
        using var db = new WarehouseDbContext();
        var customerRoleId = db.Roles.Where(r => r.Name == "Customer").Select(r => r.RoleId).FirstOrDefault();
        return db.Accounts
            .Where(a => a.IsActive && a.RoleId == customerRoleId)
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
            .Where(o => o.OrderType == WmsStatus.OrderType.Out)
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
                UnitPrice = l.UnitPrice
            })
            .ToList();
    }

    public List<FefoCandidate> PreviewAllocation(int productId, int customerId)
    {
        int minShelfLifeDays = _allocationService.GetMinShelfLifeDays(customerId);
        return _allocationService.GetFefoCandidates(productId, minShelfLifeDays);
    }

    public int CreateOrder(string orderNo, int customerId,
                           DateTime? scheduledStart, DateTime? scheduledEnd,
                           string? note, List<OrderLineInput> lines)
    {
        return _allocationService.CreateOutboundOrder(
            orderNo, customerId, scheduledStart, scheduledEnd, note, _session.AccountId, lines);
    }

    public void Allocate(int orderId)
    {
        _allocationService.AllocateOrder(orderId, _session.AccountId);
    }
}
