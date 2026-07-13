using System;
using System.Collections.Generic;

namespace RalseiWarehouse.Models;

public partial class OutputInfo
{
    public string Id { get; set; } = null!;

    public string ObjectId { get; set; } = null!;

    public string OutputId { get; set; } = null!;

    public int CustomerId { get; set; }

    public int Count { get; set; }

    public string? Status { get; set; }

    public virtual Customer Customer { get; set; } = null!;

    public virtual Object Object { get; set; } = null!;

    public virtual Output Output { get; set; } = null!;
}
