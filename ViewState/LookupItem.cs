using System;

namespace RalseiWarehouse_v2.ViewState;

// Generic item for ComboBoxes (parties, products, units, locations).
public class LookupItem
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}

// FEFO allocation candidate shown before/during allocation.
public class FefoCandidate
{
    public int StockId { get; set; }
    public int LocationId { get; set; }
    public string? LotNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public int QtyAvailable { get; set; }
}
