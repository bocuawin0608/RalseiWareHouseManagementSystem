using System;
using System.Collections.Generic;

namespace RalseiWarehouse.Models;

public partial class Unit
{
    public int UnitId { get; set; }

    public string? DisplayName { get; set; }

    public virtual ICollection<Object> Objects { get; set; } = new List<Object>();
}
