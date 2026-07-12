using System;
using System.Collections.Generic;

namespace RalseiWarehouse.Models;

public partial class Output
{
    public string OutputId { get; set; } = null!;

    public DateTime? DateOutput { get; set; }

    public virtual ICollection<OutputInfo> OutputInfos { get; set; } = new List<OutputInfo>();
}
