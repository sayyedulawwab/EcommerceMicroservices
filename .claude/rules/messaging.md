---
paths:
  - "src/SharedKernel/Events/**"
  - "src/*/*.Application/**/Events/**"
  - "src/*/*.Application/**/Notifications/**"
  - "src/Payment/**"
  - "src/Ordering/**"
  - "src/Catalog/**"
  - "src/Cart/**"
  - "src/*/*.API/Program.cs"
  - "tests/IntegrationTests/**"
---

# Integration events (MassTransit + RabbitMQ)

Event contracts live in `SharedKernel/Events`. Consumers implement `IIntegrationEventHandler<T>` (a MassTransit `IConsumer<T>`) and are registered explicitly with `AddConsumer<>` in each service's `Program.cs`. You must add any new consumer there. Queue names are prefixed with the service name (`KebabCaseEndpointNameFormatter("catalog", false)`), so each service gets its own copy of an event. Consumers log a failed `Result` and ack the message. Exceptions are retried (exponential, 5 times) and then go to the `_error` queue.

Order flow (a choreographed saga). The client sends only product ids and quantities:

1. Ordering `PlaceOrder` → `OrderPlacedIntegrationEvent` (status `Placed`, no prices yet)
2. Catalog reserves stock (decrements `Quantity`, writes `StockReservations`) and answers `OrderItemsStockConfirmed` with Catalog's names and prices, or `OrderItemsStockRejected` (unknown product, not enough stock, mixed currencies)
3. Ordering prices the order and computes the total → `StockConfirmed`, publishes `OrderStatusChangedToStockConfirmed` (with amount); on rejection → `Cancelled` + `OrderCancelled`
4. Payment.Processor charges the amount → `OrderPaymentSucceeded` / `OrderPaymentFailed`
5. Ordering → `Paid` + `OrderStatusChangedToPaid` (Catalog deletes the reservations, Cart clears the cart); on failure → `Cancelled` + `OrderCancelled` (Catalog releases the reserved stock)
6. Admin moves `Paid → Shipped → Delivered`. Customers may cancel while `Placed`/`StockConfirmed`.

Every cancellation publishes `OrderCancelled`, and releasing stock is idempotent. If stock gets confirmed for an already-cancelled order, Ordering publishes `OrderCancelled` again. A payment that succeeds for an order the customer cancelled meanwhile is only logged. There are no refunds.

**Outbox:** Catalog and Ordering use MassTransit's EF Core outbox and inbox (tables in their DBs). Publish before `SaveChangesAsync` so the event is stored in the same transaction. Inside consumers this happens automatically. `PlaceOrder` publishes `OrderPlaced` and saves once: the order ID exists before the save, so the order and its outbox message commit together. Cart and Payment have no database: Cart's handler is idempotent, and Payment uses the in-memory outbox.
