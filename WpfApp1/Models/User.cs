using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RalseiWarehouse.Models;

/// <summary>
/// Represents an application user with login credentials and role-based access.
/// </summary>
[Table("User")]
public class User
{
    /// <summary>Gets or sets the unique identifier of the user.</summary>
    [Key]
    public int UserId { get; set; }

    /// <summary>Gets or sets the display name shown in the UI.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets the login username (unique).</summary>
    [MaxLength(100)]
    public string? UserName { get; set; }

    /// <summary>Gets or sets the plain-text password.</summary>
    public string? Password { get; set; }

    /// <summary>Gets or sets the foreign key to the user's role.</summary>
    public int RoleId { get; set; }

    /// <summary>Gets or sets the associated role.</summary>
    public Role? Role { get; set; }
}
