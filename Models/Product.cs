using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RalseiWarehouse_v2.Models;

[Table("Product")]
public class Product
{
    [Key]
    public int ProductId { get; set; }

    [Required, StringLength(50)]
    public string SKU { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    public int UnitId { get; set; }          // base (smallest) unit

    public int SupplierId { get; set; }      // default supplier account

    [StringLength(64)]
    public string? BarCode { get; set; }

    public int? UnitsPerCarton { get; set; }
    public int? CartonsPerPallet { get; set; }
    public int? ShelfLifeDays { get; set; }  // sets ExpiryDate at receiving

    public bool IsActive { get; set; }

    [ForeignKey(nameof(UnitId))]
    public Unit? Unit { get; set; }

    [ForeignKey(nameof(SupplierId))]
    public Account? Supplier { get; set; }
}
