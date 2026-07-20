using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RalseiWarehouse.Models;

/// <summary>
/// Represents a customer who receives products from the warehouse.
/// </summary>
[Table("Customer")]
public class Customer
{
    /// <summary>Gets or sets the unique identifier of the customer.</summary>
    [Key]
    public int CustomerId { get; set; }

    /// <summary>Gets or sets the customer name.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets the customer address.</summary>
    public string? Address { get; set; }

    /// <summary>Gets or sets the customer phone number.</summary>
    public string? Phone { get; set; }

    /// <summary>Gets or sets the customer email address.</summary>
    public string? Email { get; set; }

    /// <summary>Gets or sets additional notes about the customer.</summary>
    public string? MoreInfo { get; set; }

    /// <summary>Gets or sets the contract date with the customer.</summary>
    public DateTime? ContractDate { get; set; }

    /// <summary>Gets or sets the output records associated with this customer.</summary>
    public ICollection<OutputInfo> OutputInfos { get; set; } = new List<OutputInfo>();
}
