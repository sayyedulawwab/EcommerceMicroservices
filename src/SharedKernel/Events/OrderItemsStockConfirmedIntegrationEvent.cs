﻿namespace SharedKernel.Events;
public record OrderItemsStockConfirmedIntegrationEvent(long OrderId) : IEvent;