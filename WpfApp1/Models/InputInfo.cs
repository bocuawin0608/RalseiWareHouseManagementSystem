using System;
using System.Collections.Generic;

namespace RalseiWarehouse.Models;

public partial class InputInfo
{
    public string Id { get; set; } = null!;

    public string ObjectId { get; set; } = null!;

    public string InputId { get; set; } = null!;

    public int Count { get; set; }

    public double? InputPrice { get; set; }

    public double? OutputPrice { get; set; }

    public string? Status { get; set; }

    public virtual Input Input { get; set; } = null!;

    public virtual Object Object { get; set; } = null!;
}
