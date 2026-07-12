using System;
using System.Collections.Generic;

namespace RalseiWarehouse.Models;

public partial class Object
{
    public string Id { get; set; } = null!;

    public string? DisplayName { get; set; }

    public int UnitId { get; set; }

    public int SupplierId { get; set; }

    public string? QRCode { get; set; }

    public string? BarCode { get; set; }

    public virtual ICollection<InputInfo> InputInfos { get; set; } = new List<InputInfo>();

    public virtual ICollection<OutputInfo> OutputInfos { get; set; } = new List<OutputInfo>();

    public virtual Supplier Supplier { get; set; } = null!;

    public virtual Unit Unit { get; set; } = null!;
}
