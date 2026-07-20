using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RalseiWarehouse.Models;

/// <summary>
/// Represents a single line item within an export (stock-out) receipt.
/// </summary>
[Table("OutputInfo")]
public class OutputInfo
{
    /// <summary>Gets or sets the unique identifier of the line item.</summary>
    [Key]
    [MaxLength(128)]
    public string Id { get; set; } = null!;

    /// <summary>Gets or sets the foreign key to the product.</summary>
    [MaxLength(128)]
    public string ObjectId { get; set; } = null!;

    /// <summary>Gets or sets the foreign key to the export receipt.</summary>
    [MaxLength(128)]
    public string OutputId { get; set; } = null!;

    /// <summary>Gets or sets the foreign key to the customer.</summary>
    public int CustomerId { get; set; }

    /// <summary>Gets or sets the quantity shipped.</summary>
    public int Count { get; set; }

    /// <summary>Gets or sets the status of the line item.</summary>
    public string? Status { get; set; }

    /// <summary>Gets or sets the associated product.</summary>
    public Object? Object { get; set; }

    /// <summary>Gets or sets the associated export receipt.</summary>
    public Output? Output { get; set; }

    /// <summary>Gets or sets the associated customer.</summary>
    public Customer? Customer { get; set; }
}
