using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RalseiWarehouse.Models;

/// <summary>
/// Represents a product (object) in the warehouse inventory.
/// </summary>
[Table("Object")]
public class Object
{
    /// <summary>Gets or sets the unique identifier of the product.</summary>
    [Key]
    [MaxLength(128)]
    public string Id { get; set; } = null!;

    /// <summary>Gets or sets the product name.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets the foreign key to the unit of measurement.</summary>
    public int UnitId { get; set; }

    /// <summary>Gets or sets the foreign key to the supplier.</summary>
    public int SupplierId { get; set; }

    /// <summary>Gets or sets the QR code for the product.</summary>
    public string? QRCode { get; set; }

    /// <summary>Gets or sets the barcode for the product.</summary>
    public string? BarCode { get; set; }

    /// <summary>Gets or sets the associated unit.</summary>
    public Unit? Unit { get; set; }

    /// <summary>Gets or sets the associated supplier.</summary>
    public Supplier? Supplier { get; set; }

    /// <summary>Gets or sets the input records for this product.</summary>
    public ICollection<InputInfo> InputInfos { get; set; } = new List<InputInfo>();

    /// <summary>Gets or sets the output records for this product.</summary>
    public ICollection<OutputInfo> OutputInfos { get; set; } = new List<OutputInfo>();
}
