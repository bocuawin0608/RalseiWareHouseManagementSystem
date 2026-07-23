using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RalseiWarehouse_v2.Models;

// ONE party table: Admin/Staff log in, Customer/Supplier only appear on documents.
[Table("Account")]
public class Account
{
    [Key]
    public int AccountId { get; set; }

    public int RoleId { get; set; }

    [Required, StringLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    [StringLength(100)]
    public string? UserName { get; set; }

    // PLAIN TEXT password for now (course requirement) - hash later
    [StringLength(256)]
    public string? PasswordHash { get; set; }

    [StringLength(20)]
    public string? Phone { get; set; }

    [StringLength(200)]
    public string? Email { get; set; }

    [StringLength(400)]
    public string? Address { get; set; }

    [Column(TypeName = "date")]
    public DateTime? ContractDate { get; set; }

    // Customer-specific pack/lot/expiry/label rules as JSON (Role = Customer only)
    public string? CustomerRules { get; set; }

    public bool IsActive { get; set; }

    [ForeignKey(nameof(RoleId))]
    public Role? Role { get; set; }
}
