using System;

namespace RalseiWarehouse_v2.ViewState;

public class StockRowDisplay
{
    public int StockId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string LocationCode { get; set; } = string.Empty;
    public string? LotNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? HandlingUnit { get; set; }
    public string QualityStatus { get; set; } = string.Empty;
    public int QtyOnHand { get; set; }
    public int QtyReserved { get; set; }
    public int QtyAvailable { get; set; }
}
