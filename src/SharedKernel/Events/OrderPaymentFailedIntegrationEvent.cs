﻿namespace SharedKernel.Events;

public record OrderPaymentSucceededIntegrationEvent(long OrderId) : IIntegrationEvent;