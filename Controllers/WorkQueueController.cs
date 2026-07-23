using System;
using System.Collections.Generic;
using System.Linq;
using RalseiWarehouse_v2.Data;
using RalseiWarehouse_v2.Models;
using RalseiWarehouse_v2.Services;
using RalseiWarehouse_v2.Session;
using RalseiWarehouse_v2.ViewState;

namespace RalseiWarehouse_v2.Controllers;

// The worker's "next task" board. Physical work executes through the services.
public class WorkQueueController
{
    private readonly UserSession _session;
    private readonly IReceivingService _receivingService = new ReceivingService();
    private readonly IShippingService _shippingService = new ShippingService();

    public WorkQueueController(UserSession session) => _session = session;

    public (List<WorkTaskItem> Tasks, int TotalInDb, int OpenCount) GetOpenTasks()
    {
        using var db = new WarehouseDbContext();
        var products = db.Products.ToDictionary(p => p.ProductId, p => p.DisplayName);
        var locations = db.Locations.ToDictionary(l => l.LocationId, l => l.Code);
        var workers = db.Accounts.ToDictionary(a => a.AccountId, a => a.DisplayName);
        var stocks = db.Stocks.ToDictionary(s => s.StockId);
        var lines = db.OrderLines.ToDictionary(l => l.OrderLineId);

        int totalInDb = db.WorkTasks.Count();
        int openCount = db.WorkTasks.Count(t => t.Status == WmsStatus.Task.Open);

        var tasks = db.WorkTasks
            .Where(t => t.Status == WmsStatus.Task.Open
                     || t.Status == WmsStatus.Task.Assigned
                     || t.Status == WmsStatus.Task.InProgress)
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.CreatedAt)
            .ToList();

        return (tasks.Select(t => new WorkTaskItem
        {
            TaskId = t.TaskId,
            TaskType = t.TaskType,
            Status = t.Status,
            Priority = t.Priority,
            Info = BuildInfo(t, lines, stocks, products, locations),
            AssignedToName = t.AssignedTo.HasValue ? workers.GetValueOrDefault(t.AssignedTo.Value, "?") : null,
            CreatedAt = t.CreatedAt
        }).ToList(), totalInDb, openCount);
    }

    public List<LookupItem> GetStorageLocations()
    {
        using var db = new WarehouseDbContext();
        return db.Locations
            .Where(l => l.IsActive && l.LocationType == "Storage")
            .Select(l => new LookupItem { Id = l.LocationId, DisplayName = l.Code })
            .ToList();
    }

    public List<LookupItem> GetWorkers()
    {
        using var db = new WarehouseDbContext();
        var staffRoleId = db.Roles.Where(r => r.Name == "Staff").Select(r => r.RoleId).First();
        return db.Accounts
            .Where(a => a.IsActive && a.RoleId == staffRoleId)
            .Select(a => new LookupItem { Id = a.AccountId, DisplayName = a.DisplayName })
            .ToList();
    }

    public void AssignToWorker(long taskId, int workerAccountId)
    {
        using var db = new WarehouseDbContext();
        var task = FindOpenTask(db, taskId);
        task.AssignedTo = workerAccountId;
        task.Status = WmsStatus.Task.Assigned;
        db.SaveChanges();
    }

    // --- queue housekeeping (no stock effect, safe at controller level) ---

    public void AssignToMe(long taskId)
    {
        using var db = new WarehouseDbContext();
        var task = FindOpenTask(db, taskId);
        task.AssignedTo = _session.AccountId;
        task.Status = WmsStatus.Task.Assigned;
        db.SaveChanges();
    }

    public void StartTask(long taskId)
    {
        using var db = new WarehouseDbContext();
        var task = FindOpenTask(db, taskId);
        task.Status = WmsStatus.Task.InProgress;
        task.StartedAt = DateTime.Now;
        db.SaveChanges();
    }

    public void CancelTask(long taskId)
    {
        using var db = new WarehouseDbContext();
        var task = FindOpenTask(db, taskId);
        task.Status = WmsStatus.Task.Cancelled;
        task.CompletedAt = DateTime.Now;
        db.SaveChanges();
    }

    // --- execution: dispatches to the golden-rule services ---

    public void ExecuteReceive(long taskId, string? lot, DateTime? expiry, string? handlingUnit, int qty)
        => _receivingService.ExecuteReceive(taskId, lot, expiry, handlingUnit, qty, _session.AccountId);

    public void ExecutePutaway(long taskId, int toLocationId)
        => _receivingService.ExecutePutaway(taskId, toLocationId, _session.AccountId);

    public void ExecutePick(long taskId)
        => _shippingService.ExecutePick(taskId, _session.AccountId);

    public void ExecutePack(long taskId, string? handlingUnit)
        => _shippingService.ExecutePack(taskId, handlingUnit, _session.AccountId);

    public void ExecuteShip(long taskId)
        => _shippingService.ExecuteShip(taskId, _session.AccountId);

    private static WorkTask FindOpenTask(WarehouseDbContext db, long taskId)
    {
        var task = db.WorkTasks.Find(taskId)
            ?? throw new InvalidOperationException("Task not found.");
        if (task.Status is WmsStatus.Task.Done or WmsStatus.Task.Cancelled)
            throw new InvalidOperationException("Task is already closed.");
        return task;
    }

    private static string BuildInfo(WorkTask t,
        Dictionary<int, OrderLine> lines,
        Dictionary<int, Stock> stocks,
        Dictionary<int, string> products,
        Dictionary<int, string> locations)
    {
        string productName = "?";
        if (t.OrderLineId.HasValue && lines.TryGetValue(t.OrderLineId.Value, out var line))
            productName = products.GetValueOrDefault(line.ProductId, "?");
        else if (t.StockId.HasValue && stocks.TryGetValue(t.StockId.Value, out var stock))
            productName = products.GetValueOrDefault(stock.ProductId, "?");

        string from = t.FromLocationId.HasValue ? locations.GetValueOrDefault(t.FromLocationId.Value, "?") : "dock";
        string to = t.ToLocationId.HasValue ? locations.GetValueOrDefault(t.ToLocationId.Value, "?") : "...";

        return t.TaskType switch
        {
            WmsStatus.TaskType.Receive => $"Receive {t.Qty} x {productName} at {to}",
            WmsStatus.TaskType.Putaway => $"Put away {t.Qty} x {productName} from {from}",
            WmsStatus.TaskType.Pick => $"Pick {t.Qty} x {productName} from {from}",
            WmsStatus.TaskType.Pack => $"Pack {t.Qty} x {productName} at {from}",
            WmsStatus.TaskType.Load => $"Load & ship {t.Qty} x {productName} from {from}",
            _ => $"{t.TaskType} {t.Qty} x {productName}"
        };
    }
}
