namespace RalseiWarehouse_v2.ViewState;

// One pending line the user typed into the order form, before saving.
public class OrderLineInput
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;   // display only
    public int UnitId { get; set; }
    public string UnitName { get; set; } = string.Empty;      // display only
    public int Qty { get; set; }
    public decimal? UnitPrice { get; set; }
}
