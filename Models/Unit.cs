using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RalseiWarehouse_v2.Models;

[Table("Unit")]
public class Unit
{
    [Key]
    public int UnitId { get; set; }

    [Required, StringLength(50)]
    public string DisplayName { get; set; } = string.Empty;
}
