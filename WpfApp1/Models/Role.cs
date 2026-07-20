using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RalseiWarehouse.Models;

/// <summary>
/// Represents a user role that determines access privileges.
/// </summary>
[Table("Role")]
public class Role
{
    /// <summary>Gets or sets the unique identifier of the role.</summary>
    [Key]
    public int RoleId { get; set; }

    /// <summary>Gets or sets the display name of the role.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets the users assigned to this role.</summary>
    public ICollection<User> Users { get; set; } = new List<User>();
}
