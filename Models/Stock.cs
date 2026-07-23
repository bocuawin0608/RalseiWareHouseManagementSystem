using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RalseiWarehouse_v2.Models;

// Physical stock: one row per unique (product, location, lot, serial, pallet,
// quality, owner, receipt). Rows are zeroed, never deleted.
[Table("Stock")]
public class Stock
{
    [Key]
    public int StockId { get; set; }

    public int ProductId { get; set; }
    public int LocationId { get; set; }

    [StringLength(50)]
    public string? LotNumber { get; set; }

    [StringLength(50)]
    public string? SerialNumber { get; set; }

    [StringLength(50)]
    public string? HandlingUnit { get; set; }   // pallet/carton LPN

    [Column(TypeName = "date")]
    public DateTime? ExpiryDate { get; set; }

    [Required, StringLength(20)]
    public string QualityStatus { get; set; } = WmsStatus.Quality.Available;

    public int? OwnerId { get; set; }           // stock ownership (NULL = own)
    public int? ReceiptLineId { get; set; }     // receipt that brought it in

    public int QtyOnHand { get; set; }
    public int QtyReserved { get; set; }

    [Timestamp]                                  // optimistic concurrency
    public byte[] RowVer { get; set; } = Array.Empty<byte>();

    [ForeignKey(nameof(ProductId))]
    public Product? Product { get; set; }

    [ForeignKey(nameof(LocationId))]
    public Location? Location { get; set; }
}
