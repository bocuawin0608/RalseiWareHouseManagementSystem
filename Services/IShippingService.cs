namespace RalseiWarehouse_v2.Services;

// Workflow B steps 3-6: pick to staging, pack pallets, confirm dispatch.
public interface IShippingService
{
    // Golden-rule write: consume reservation, move qty shelf -> staging,
    // Pick movement, open Pack task. One transaction.
    void ExecutePick(long taskId, int performedBy);

    // Golden-rule write: assign handling unit (pallet id) on staged stock.
    void ExecutePack(long taskId, string? handlingUnit, int performedBy);

    // Golden-rule write: staged qty leaves the building (Ship movement, To = NULL),
    // line/order progress updated to Completed when fully shipped.
    void ExecuteShip(long taskId, int performedBy);
}
