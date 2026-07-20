using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RalseiWarehouse.Models;

/// <summary>
/// Represents an export (stock-out) receipt header.
/// </summary>
[Table("Output")]
public class Output
{
    /// <summary>Gets or sets the unique identifier of the export receipt.</summary>
    [Key]
    [MaxLength(128)]
    public string OutputId { get; set; } = null!;

    /// <summary>Gets or sets the date and time of the export.</summary>
    public DateTime? DateOutput { get; set; }

    /// <summary>Gets or sets the line items of this export receipt.</summary>
    public ICollection<OutputInfo> OutputInfos { get; set; } = new List<OutputInfo>();
}
