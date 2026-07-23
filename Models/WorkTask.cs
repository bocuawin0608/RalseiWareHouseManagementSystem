using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RalseiWarehouse_v2.Models;

// Worker task assignment. Named WorkTask to avoid clashing with C# Task.
[Table("WorkTask")]
public class WorkTask
{
    [Key]
    public long TaskId { get; set; }

    [Required, StringLength(20)]
    public string TaskType { get; set; } = string.Empty;   // Receive/Putaway/Pick/Pack/Stage/Load/Count

    [Required, StringLength(20)]
    public string Status { get; set; } = WmsStatus.Task.Open;

    public int Priority { get; set; }

    public int? OrderLineId { get; set; }
    public int? StockId { get; set; }
    public int? FromLocationId { get; set; }
    public int? ToLocationId { get; set; }
    public int? Qty { get; set; }
    public int? AssignedTo { get; set; }       // worker (Role = Staff)

    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    [ForeignKey(nameof(OrderLineId))]
    public OrderLine? OrderLine { get; set; }

    [ForeignKey(nameof(StockId))]
    public Stock? Stock { get; set; }

    [ForeignKey(nameof(FromLocationId))]
    public Location? FromLocation { get; set; }

    [ForeignKey(nameof(ToLocationId))]
    public Location? ToLocation { get; set; }

    [ForeignKey(nameof(AssignedTo))]
    public Account? AssignedToAccount { get; set; }
}
