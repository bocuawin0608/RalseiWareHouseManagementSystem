namespace RalseiWarehouse_v2.ViewState;

public class OrderLineItem
{
    public int OrderLineId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public int QtyOrdered { get; set; }
    public int QtyAllocated { get; set; }
    public int QtyPicked { get; set; }
    public int QtyShipped { get; set; }
    public int ReceivedQty { get; set; }   // IN orders: from the audit ledger
    public decimal? UnitPrice { get; set; }
}
