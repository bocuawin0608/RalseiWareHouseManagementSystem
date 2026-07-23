using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RalseiWarehouse_v2.Models;

// ONE document type for inbound receipts (IN) and outbound shipments (OUT).
[Table("OrderHeader")]
public class OrderHeader
{
    [Key]
    public int OrderId { get; set; }

    [Required, StringLength(20)]
    public string OrderNo { get; set; } = string.Empty;

    [Required, StringLength(3)]
    public string OrderType { get; set; } = WmsStatus.OrderType.In;

    public int AccountId { get; set; }       // supplier (IN) / customer (OUT)

    [Required, StringLength(20)]
    public string Status { get; set; } = WmsStatus.Order.Draft;

    public DateTime? ScheduledStart { get; set; }   // collection / delivery window
    public DateTime? ScheduledEnd { get; set; }

    [StringLength(400)]
    public string? Note { get; set; }

    public int? CreatedBy { get; set; }      // staff member
    public DateTime CreatedAt { get; set; }

    [ForeignKey(nameof(AccountId))]
    public Account? Account { get; set; }

    [ForeignKey(nameof(CreatedBy))]
    public Account? CreatedByAccount { get; set; }
}
