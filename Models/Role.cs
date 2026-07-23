using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RalseiWarehouse_v2.Models;

[Table("Role")]
public class Role
{
    [Key]
    public int RoleId { get; set; }

    [Required, StringLength(50)]
    public string Name { get; set; } = string.Empty;
}
