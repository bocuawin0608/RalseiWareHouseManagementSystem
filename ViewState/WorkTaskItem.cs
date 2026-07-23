using System;

namespace RalseiWarehouse_v2.ViewState;

public class WorkTaskItem
{
    public long TaskId { get; set; }
    public string TaskType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Priority { get; set; }
    public string Info { get; set; } = string.Empty;   // "Pick 80 x Green Tea from A-01-02"
    public string? AssignedToName { get; set; }
    public DateTime CreatedAt { get; set; }
}
