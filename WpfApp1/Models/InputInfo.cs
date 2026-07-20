using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RalseiWarehouse.Models;

/// <summary>
/// Represents a single line item within an import (stock-in) receipt.
/// </summary>
[Table("InputInfo")]
public class InputInfo
{
    /// <summary>Gets or sets the unique identifier of the line item.</summary>
    [Key]
    [MaxLength(128)]
    public string Id { get; set; } = null!;

    /// <summary>Gets or sets the foreign key to the product.</summary>
    [MaxLength(128)]
    public string ObjectId { get; set; } = null!;

    /// <summary>Gets or sets the foreign key to the import receipt.</summary>
    [MaxLength(128)]
    public string InputId { get; set; } = null!;

    /// <summary>Gets or sets the quantity received.</summary>
    public int Count { get; set; }

    /// <summary>Gets or sets the purchase price per unit.</summary>
    public double? InputPrice { get; set; }

    /// <summary>Gets or sets the selling price per unit.</summary>
    public double? OutputPrice { get; set; }

    /// <summary>Gets or sets the status of the line item.</summary>
    public string? Status { get; set; }

    /// <summary>Gets or sets the associated product.</summary>
    public Object? Object { get; set; }

    /// <summary>Gets or sets the associated import receipt.</summary>
    public Input? Input { get; set; }
}
