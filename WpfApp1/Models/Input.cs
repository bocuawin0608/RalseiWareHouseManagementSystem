using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RalseiWarehouse.Models;

/// <summary>
/// Represents an import (stock-in) receipt header.
/// </summary>
[Table("Input")]
public class Input
{
    /// <summary>Gets or sets the unique identifier of the import receipt.</summary>
    [Key]
    [MaxLength(128)]
    public string InputId { get; set; } = null!;

    /// <summary>Gets or sets the date and time of the import.</summary>
    public DateTime? DateInput { get; set; }

    /// <summary>Gets or sets the line items of this import receipt.</summary>
    public ICollection<InputInfo> InputInfos { get; set; } = new List<InputInfo>();
}
