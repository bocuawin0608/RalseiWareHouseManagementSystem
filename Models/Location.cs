using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RalseiWarehouse_v2.Models;

[Table("Location")]
public class Location
{
    [Key]
    public int LocationId { get; set; }

    [Required, StringLength(20)]
    public string Code { get; set; } = string.Empty;   // RCV-01, A-01-01, STG-01...

    [Required, StringLength(20)]
    public string LocationType { get; set; } = string.Empty;   // Storage/Receiving/Staging/Shipping/Quarantine

    [StringLength(20)]
    public string? Zone { get; set; }

    public bool IsActive { get; set; }
}
