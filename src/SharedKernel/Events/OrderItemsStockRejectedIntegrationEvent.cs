﻿namespace SharedKernel.Events;
public record OrderItemsStockRejectedIntegrationEvent(long OrderId) : IEvent;