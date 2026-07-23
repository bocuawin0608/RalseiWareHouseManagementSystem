using System;

namespace RalseiWarehouse_v2.ViewState;

// Bindable DTO for order grids - controllers push these into views,
// views never bind EF Models directly.
public class OrderListItem
{
    public int OrderId { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public string OrderType { get; set; } = string.Empty;
    public string PartyName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? ScheduledStart { get; set; }
    public DateTime? ScheduledEnd { get; set; }
    public int LineCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
