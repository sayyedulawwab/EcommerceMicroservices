﻿namespace SharedKernel.Events;
public record OrderStatusChangedToStockConfirmedIntegrationEvent(long OrderId) : IEvent;