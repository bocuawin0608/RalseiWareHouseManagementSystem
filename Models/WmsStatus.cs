namespace RalseiWarehouse_v2.Models;

// String constants matching the CHECK constraints in sql/ddl.sql.
public static class WmsStatus
{
    public static class OrderType
    {
        public const string In = "IN";
        public const string Out = "OUT";
    }

    public static class Order
    {
        public const string Draft = "Draft";
        public const string Confirmed = "Confirmed";
        public const string InProgress = "InProgress";
        public const string Completed = "Completed";
        public const string Cancelled = "Cancelled";
    }

    public static class Quality
    {
        public const string Available = "Available";
        public const string Quarantine = "Quarantine";
        public const string Damaged = "Damaged";
        public const string Hold = "Hold";
    }

    public static class Task
    {
        public const string Open = "Open";
        public const string Assigned = "Assigned";
        public const string InProgress = "InProgress";
        public const string Done = "Done";
        public const string Cancelled = "Cancelled";
    }

    public static class TaskType
    {
        public const string Receive = "Receive";
        public const string Putaway = "Putaway";
        public const string Pick = "Pick";
        public const string Pack = "Pack";
        public const string Stage = "Stage";
        public const string Load = "Load";
        public const string Count = "Count";
    }

    public static class Move
    {
        public const string Receipt = "Receipt";
        public const string Putaway = "Putaway";
        public const string Pick = "Pick";
        public const string Pack = "Pack";
        public const string MoveStock = "Move";
        public const string Ship = "Ship";
        public const string Adjust = "Adjust";
        public const string Damage = "Damage";
        public const string Quarantine = "Quarantine";
        public const string Release = "Release";
        public const string Return = "Return";
        public const string CountAdjust = "CountAdjust";
    }
}
