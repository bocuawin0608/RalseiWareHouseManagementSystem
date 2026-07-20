using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RalseiWarehouse.Models;

/// <summary>
/// Represents a supplier that provides products to the warehouse.
/// </summary>
[Table("Supplier")]
public class Supplier
{
    /// <summary>Gets or sets the unique identifier of the supplier.</summary>
    [Key]
    public int SupplierId { get; set; }

    /// <summary>Gets or sets the supplier name.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets the supplier address.</summary>
    public string? Address { get; set; }

    /// <summary>Gets or sets the supplier phone number.</summary>
    public string? Phone { get; set; }

    /// <summary>Gets or sets the supplier email address.</summary>
    public string? Email { get; set; }

    /// <summary>Gets or sets additional notes about the supplier.</summary>
    public string? MoreInfo { get; set; }

    /// <summary>Gets or sets the contract date with the supplier.</summary>
    public DateTime? ContractDate { get; set; }

    /// <summary>Gets or sets the products supplied by this supplier.</summary>
    public ICollection<Object> Objects { get; set; } = new List<Object>();
}
