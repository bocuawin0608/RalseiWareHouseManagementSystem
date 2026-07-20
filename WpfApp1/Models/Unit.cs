using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RalseiWarehouse.Models;

/// <summary>
/// Represents a unit of measurement for products (e.g., pcs, kg, box).
/// </summary>
[Table("Unit")]
public class Unit
{
    /// <summary>Gets or sets the unique identifier of the unit.</summary>
    [Key]
    public int UnitId { get; set; }

    /// <summary>Gets or sets the display name of the unit.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets the products that use this unit.</summary>
    public ICollection<Object> Objects { get; set; } = new List<Object>();
}
