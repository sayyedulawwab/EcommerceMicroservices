﻿namespace SharedKernel.Events;

public record OrderPaymentFailedIntegrationEvent(long OrderId) : IIntegrationEvent;