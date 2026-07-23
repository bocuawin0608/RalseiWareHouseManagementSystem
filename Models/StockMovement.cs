using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RalseiWarehouse_v2.Models;

// Append-only audit ledger: EVERY inventory-changing action writes row(s) here.
[Table("StockMovement")]
public class StockMovement
{
    [Key]
    public long MovementId { get; set; }

    public DateTime MovementDate { get; set; }

    [Required, StringLength(20)]
    public string MovementType { get; set; } = string.Empty;

    public int StockId { get; set; }
    public int ProductId { get; set; }         // denormalized for fast product history

    public int Qty { get; set; }               // signed: + in, - out

    public int? FromLocationId { get; set; }   // NULL on Receipt
    public int? ToLocationId { get; set; }     // NULL on Ship

    public int? OrderLineId { get; set; }
    public long? TaskId { get; set; }
    public int? PerformedBy { get; set; }      // audit: who

    [StringLength(400)]
    public string? Note { get; set; }          // audit: why

    [ForeignKey(nameof(StockId))]
    public Stock? Stock { get; set; }

    [ForeignKey(nameof(ProductId))]
    public Product? Product { get; set; }

    [ForeignKey(nameof(FromLocationId))]
    public Location? FromLocation { get; set; }

    [ForeignKey(nameof(ToLocationId))]
    public Location? ToLocation { get; set; }

    [ForeignKey(nameof(OrderLineId))]
    public OrderLine? OrderLine { get; set; }

    [ForeignKey(nameof(TaskId))]
    public WorkTask? Task { get; set; }

    [ForeignKey(nameof(PerformedBy))]
    public Account? PerformedByAccount { get; set; }
}
