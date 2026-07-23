using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RalseiWarehouse_v2.Models;

[Table("OrderLine")]
public class OrderLine
{
    [Key]
    public int OrderLineId { get; set; }

    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public int UnitId { get; set; }          // ordered as Pallet/Carton/Piece

    public int QtyOrdered { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? UnitPrice { get; set; }  // price at order time

    public int QtyAllocated { get; set; }
    public int QtyPicked { get; set; }
    public int QtyShipped { get; set; }

    [ForeignKey(nameof(OrderId))]
    public OrderHeader? Order { get; set; }

    [ForeignKey(nameof(ProductId))]
    public Product? Product { get; set; }

    [ForeignKey(nameof(UnitId))]
    public Unit? Unit { get; set; }
}
