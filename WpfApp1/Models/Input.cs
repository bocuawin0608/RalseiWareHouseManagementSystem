using System;
using System.Collections.Generic;

namespace RalseiWarehouse.Models;

public partial class Input
{
    public string InputId { get; set; } = null!;

    public DateTime? DateInput { get; set; }

    public virtual ICollection<InputInfo> InputInfos { get; set; } = new List<InputInfo>();
}
