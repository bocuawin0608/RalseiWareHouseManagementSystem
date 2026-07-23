using System;
using System.Collections.Generic;
using RalseiWarehouse_v2.ViewState;

namespace RalseiWarehouse_v2.Services;

// Workflow B steps 1-2: register outbound orders, allocate stock FEFO.
public interface IStockAllocationService
{
    int CreateOutboundOrder(string orderNo, int customerId,
                            DateTime? scheduledStart, DateTime? scheduledEnd,
                            string? note, int createdBy, List<OrderLineInput> lines);

    // Reads CustomerRules JSON (minShelfLifeDays) for the customer.
    int GetMinShelfLifeDays(int customerId);

    // FEFO candidates: available stock, earliest expiry first, no-expiry last.
    List<FefoCandidate> GetFefoCandidates(int productId, int minShelfLifeDays);

    // Reserves stock for every line of the order and opens Pick tasks.
    // Throws (and saves nothing) when any line cannot be fully allocated.
    void AllocateOrder(int orderId, int performedBy);
}
