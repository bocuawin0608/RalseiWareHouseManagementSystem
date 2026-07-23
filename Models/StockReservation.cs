using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RalseiWarehouse_v2.Models;

// The "sticky note": which stock rows are promised to which order line.
[Table("StockReservation")]
public class StockReservation
{
    [Key]
    public int ReservationId { get; set; }

    public int OrderLineId { get; set; }
    public int StockId { get; set; }
    public int Qty { get; set; }
    public DateTime CreatedAt { get; set; }

    [ForeignKey(nameof(OrderLineId))]
    public OrderLine? OrderLine { get; set; }

    [ForeignKey(nameof(StockId))]
    public Stock? Stock { get; set; }
}
