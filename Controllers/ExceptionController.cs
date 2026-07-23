using System;
using System.Collections.Generic;
using System.Linq;
using RalseiWarehouse_v2.Data;
using RalseiWarehouse_v2.Models;
using RalseiWarehouse_v2.Session;
using RalseiWarehouse_v2.ViewState;

namespace RalseiWarehouse_v2.Controllers;

// Workflow C: damage, quarantine, release, cycle-count adjustments.
// Writes follow the golden rule directly here (stock + movement, one SaveChanges).
public class ExceptionController
{
    private readonly UserSession _session;

    public ExceptionController(UserSession session) => _session = session;

    public List<StockRowDisplay> GetStockRows(string? qualityFilter)
    {
        using var db = new WarehouseDbContext();
        var products = db.Products.ToDictionary(p => p.ProductId, p => p.DisplayName);
        var locations = db.Locations.ToDictionary(l => l.LocationId, l => l.Code);

        var query = db.Stocks.Where(s => s.QtyOnHand > 0);
        if (!string.IsNullOrWhiteSpace(qualityFilter) && qualityFilter != "All")
            query = query.Where(s => s.QualityStatus == qualityFilter);

        return query
            .OrderBy(s => s.ProductId).ThenBy(s => s.LocationId)
            .ToList()
            .Select(s => new StockRowDisplay
            {
                StockId = s.StockId,
                ProductName = products.GetValueOrDefault(s.ProductId, "?"),
                LocationCode = locations.GetValueOrDefault(s.LocationId, "?"),
                LotNumber = s.LotNumber,
                ExpiryDate = s.ExpiryDate,
                HandlingUnit = s.HandlingUnit,
                QualityStatus = s.QualityStatus,
                QtyOnHand = s.QtyOnHand,
                QtyReserved = s.QtyReserved,
                QtyAvailable = s.QtyOnHand - s.QtyReserved
            })
            .ToList();
    }

    public List<LookupItem> GetStorageLocations()
    {
        using var db = new WarehouseDbContext();
        return db.Locations
            .Where(l => l.IsActive && l.LocationType == "Storage")
            .Select(l => new LookupItem { Id = l.LocationId, DisplayName = l.Code })
            .ToList();
    }

    // Damaged goods are moved to the quarantine area with status Damaged.
    public void ReportDamage(int stockId, int qty, string note)
        => MoveToQuarantine(stockId, qty, WmsStatus.Quality.Damaged, WmsStatus.Move.Damage, note);

    public void Quarantine(int stockId, int qty, string note)
        => MoveToQuarantine(stockId, qty, WmsStatus.Quality.Quarantine, WmsStatus.Move.Quarantine, note);

    // QC passed: the whole row goes back to a storage location as Available.
    public void Release(int stockId, int toLocationId)
    {
        using var db = new WarehouseDbContext();
        var stock = db.Stocks.Find(stockId)
            ?? throw new InvalidOperationException("Stock row not found.");
        if (stock.QualityStatus == WmsStatus.Quality.Available)
            throw new InvalidOperationException("Stock is already Available.");

        var destination = db.Locations.Find(toLocationId)
            ?? throw new InvalidOperationException("Destination location not found.");

        db.StockMovements.Add(new StockMovement
        {
            MovementDate = DateTime.Now,
            MovementType = WmsStatus.Move.Release,
            StockId = stock.StockId,
            ProductId = stock.ProductId,
            Qty = stock.QtyOnHand,
            FromLocationId = stock.LocationId,
            ToLocationId = toLocationId,
            PerformedBy = _session.AccountId,
            Note = "QC release"
        });

        stock.LocationId = toLocationId;
        stock.QualityStatus = WmsStatus.Quality.Available;
        db.SaveChanges();
    }

    // Cycle count: book stock is corrected to the counted qty, evidence required.
    public void CountAdjust(int stockId, int countedQty, string note)
    {
        if (string.IsNullOrWhiteSpace(note))
            throw new ArgumentException("A reason (note) is required for count adjustments.", nameof(note));
        if (countedQty < 0)
            throw new ArgumentOutOfRangeException(nameof(countedQty), "Counted qty cannot be negative.");

        using var db = new WarehouseDbContext();
        var stock = db.Stocks.Find(stockId)
            ?? throw new InvalidOperationException("Stock row not found.");
        if (countedQty < stock.QtyReserved)
            throw new InvalidOperationException(
                $"Cannot set on-hand to {countedQty}: {stock.QtyReserved} is reserved. Release reservations first.");

        int difference = countedQty - stock.QtyOnHand;
        if (difference == 0)
            return;

        stock.QtyOnHand = countedQty;

        db.StockMovements.Add(new StockMovement
        {
            MovementDate = DateTime.Now,
            MovementType = WmsStatus.Move.CountAdjust,
            StockId = stock.StockId,
            ProductId = stock.ProductId,
            Qty = difference,                    // signed: + found extra, - missing
            FromLocationId = difference < 0 ? stock.LocationId : null,
            ToLocationId = difference > 0 ? stock.LocationId : null,
            PerformedBy = _session.AccountId,
            Note = note
        });
        db.SaveChanges();
    }

    private void MoveToQuarantine(int stockId, int qty, string qualityStatus, string movementType, string note)
    {
        if (qty <= 0)
            throw new ArgumentOutOfRangeException(nameof(qty), "Qty must be positive.");
        if (string.IsNullOrWhiteSpace(note))
            throw new ArgumentException("A reason (note) is required.", nameof(note));

        using var db = new WarehouseDbContext();
        var source = db.Stocks.Find(stockId)
            ?? throw new InvalidOperationException("Stock row not found.");
        if (qty > source.QtyOnHand - source.QtyReserved)
            throw new InvalidOperationException(
                $"Only {source.QtyOnHand - source.QtyReserved} unreserved units can be moved.");

        var quarantine = db.Locations.First(l => l.LocationType == "Quarantine");

        // split: affected qty becomes a new row at QTN-01 with the new quality status
        var held = ReceivingServiceFindOrCreate(db, source, quarantine.LocationId, qualityStatus);
        held.QtyOnHand += qty;
        source.QtyOnHand -= qty;

        db.StockMovements.Add(new StockMovement
        {
            MovementDate = DateTime.Now,
            MovementType = movementType,
            Stock = held,
            ProductId = source.ProductId,
            Qty = qty,
            FromLocationId = source.LocationId,
            ToLocationId = quarantine.LocationId,
            PerformedBy = _session.AccountId,
            Note = note
        });
        db.SaveChanges();
    }

    private static Stock ReceivingServiceFindOrCreate(WarehouseDbContext db, Stock source, int locationId, string qualityStatus)
    {
        return Services.ReceivingService.FindOrCreateStockRow(db, source.ProductId, locationId,
            source.LotNumber, source.ExpiryDate, source.HandlingUnit, qualityStatus, source.ReceiptLineId);
    }
}
