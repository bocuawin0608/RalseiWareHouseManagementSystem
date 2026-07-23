using System;
using System.Collections.Generic;
using RalseiWarehouse_v2.ViewState;

namespace RalseiWarehouse_v2.Services;

// Workflow A: register inbound orders, receive goods, put away to storage.
public interface IReceivingService
{
    // Creates the IN order (Confirmed) plus one open Receive task per line.
    int CreateInboundOrder(string orderNo, int supplierId,
                           DateTime? scheduledStart, DateTime? scheduledEnd,
                           string? note, int createdBy, List<OrderLineInput> lines);

    // Golden-rule write: Stock born at receiving dock + Receipt movement + task done,
    // plus an open Putaway task for the received qty. One SaveChanges = one transaction.
    void ExecuteReceive(long taskId, string? lotNumber, DateTime? expiryDate,
                        string? handlingUnit, int qty, int performedBy);

    // Golden-rule write: move qty from receiving to a storage location.
    void ExecutePutaway(long taskId, int toLocationId, int performedBy);
}
