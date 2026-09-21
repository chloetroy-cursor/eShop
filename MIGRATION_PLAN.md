# Idempotent consumption of integration events

Publish retries on `ordering-api` can put the same integration event on the bus twice. Consumers have to tolerate that. This file is the audit and the consumption plan. It lands before the inbox code.

## What done means

Done is all of the following.

- This file lists every `IIntegrationEventHandler` in Basket.API, Catalog.API, Ordering.API, Webhooks.API, OrderProcessor, and PaymentProcessor, with a duplicate-delivery verdict.
- `IntegrationEventLogEF` has an inbox keyed by `IntegrationEvent.Id`, stored on the same `DbContext` as the handler's other writes.
- Each handler this plan migrates has a test that calls `Handle` twice with one event id and observes the side effect once.
- Handlers this plan leaves alone are unchanged.
- The new test project and `tests/Ordering.UnitTests` pass.

## Where the duplicates come from

INC-001 records publish failures for `ordering-api` from `2026-08-18T20:00:00Z` to `2026-08-18T20:30:00Z`. Monitor `mon-ordering-publish-errors`. Release `v9.1.0-demo` at `2026-08-18T20:04:00Z`. Featured request `req-order-7f3a0000`. See `docs/incidents/incident-001.md`.

`demo/incident/generated/metadata.json` is not in this workspace, so this plan does not replay that trace. The code path is enough to show how a retry becomes a second delivery. `RabbitMQEventBus.PublishAsync` retries `BrokerUnreachableException` and `SocketException` with `Delay = TimeSpan.Zero` (`src/EventBusRabbitMQ/RabbitMQEventBus.cs`). The event object, including `Id`, is captured before the retry loop. If the broker accepted the first publish and the client still threw, the second attempt publishes the same body again. `OnMessageReceived` acks the message even when the handler throws, so the consumer does not nack a duplicate. The duplicate arrives as a second delivery of the same id.

This plan does not change that retry loop.

## How a handler was judged

A handler is unsafe when a second call with the same `IntegrationEvent.Id`, after the first call finished, repeats a write or an outbound call. A status method that returns without a new domain event is safe for that sequential case. A handler that publishes a new integration event allocates a new id, so downstream inbox checks on the original id do not see the copy.

## Inventory

| Service | Handler | Side effect on a second delivery of the same id | Rank |
| --- | --- | --- | --- |
| Catalog.API | `OrderStatusChangedToPaidIntegrationEventHandler` | `CatalogItem.RemoveStock` subtracts again. No status guard. | Critical |
| Webhooks.API | `OrderStatusChangedToPaidIntegrationEventHandler` | `WebhooksSender.SendAll` POSTs again. | Critical |
| Webhooks.API | `OrderStatusChangedToShippedIntegrationEventHandler` | `WebhooksSender.SendAll` POSTs again. | Critical |
| Catalog.API | `OrderStatusChangedToAwaitingValidationIntegrationEventHandler` | Builds a new `OrderStockConfirmedIntegrationEvent` or `OrderStockRejectedIntegrationEvent` and publishes it. Ordering's status methods ignore the copy once the order has left `AwaitingValidation`. The new id still fans out. | High |
| PaymentProcessor | `OrderStatusChangedToStockConfirmedIntegrationEventHandler` | Publishes a new payment event with a new id. Ordering's `SetPaidStatus` ignores the copy once the order is `Paid`. | High |
| Ordering.API | `OrderPaymentFailedIntegrationEventHandler` | `Order.SetCancelledStatus` does not return early when the order is already `Cancelled`. It adds `OrderCancelledDomainEvent` again, and that handler writes another `OrderStatusChangedToCancelledIntegrationEvent`. | High |
| Ordering.API | `GracePeriodConfirmedIntegrationEventHandler` | `SetAwaitingValidationStatus` acts only while the order is `Submitted`. The second call adds no domain event. | Low |
| Ordering.API | `OrderStockConfirmedIntegrationEventHandler` | `SetStockConfirmedStatus` acts only while the order is `AwaitingValidation`. | Low |
| Ordering.API | `OrderStockRejectedIntegrationEventHandler` | `SetCancelledStatusWhenStockIsRejected` acts only while the order is `AwaitingValidation`. | Low |
| Ordering.API | `OrderPaymentSucceededIntegrationEventHandler` | `SetPaidStatus` acts only while the order is `StockConfirmed`. | Low |
| Basket.API | `OrderStartedIntegrationEventHandler` | `RedisBasketRepository.DeleteBasketAsync` deletes a key. A missing key is a no-op. | Low |
| Webhooks.API | `ProductPriceChangedIntegrationEventHandler` | `Handle` returns `Task.CompletedTask`. | None |
| OrderProcessor | none | `GracePeriodManagerService` publishes `GracePeriodConfirmedIntegrationEvent`. It does not implement `IIntegrationEventHandler`. | None |

`SetAwaitingValidationStatus`, `SetStockConfirmedStatus`, `SetPaidStatus`, and `SetCancelledStatusWhenStockIsRejected` are the guards in `src/Ordering.Domain/AggregatesModel/OrderAggregate/Order.cs`. `SetCancelledStatus` is the method that still raises a domain event after the order is cancelled.

WebApp has six order-status handlers that call `OrderStatusNotificationService`. They are outside the six services in this audit. A second notify asks the client to read status again. They are not part of this change.

## What gets the inbox

Migrate every handler whose finished first delivery still repeats a side effect, and that has a `DbContext` to store the inbox on.

- `OrderStatusChangedToPaidIntegrationEventHandler` in Catalog.API
- `OrderStatusChangedToAwaitingValidationIntegrationEventHandler` in Catalog.API
- `OrderStatusChangedToPaidIntegrationEventHandler` in Webhooks.API
- `OrderStatusChangedToShippedIntegrationEventHandler` in Webhooks.API
- `OrderPaymentFailedIntegrationEventHandler` in Ordering.API

Leave the low and none rows unchanged. Their second call does not repeat the write.

Leave PaymentProcessor unchanged. It has no database and no reference to `IntegrationEventLogEF`. Adding Postgres and EF there is a new runtime dependency for a simulated payment. A second payment event with a new id is ignored by `SetPaidStatus` after the order is `Paid`. Two concurrent payment events can both observe `StockConfirmed` and both raise `OrderStatusChangedToPaid`. Those are different ids, so an event-id inbox does not collapse them. An in-memory set in PaymentProcessor would miss the same race across a restart.

## Inbox shape

The outbox already lives in `IntegrationEventLogEF`, not in `RabbitMQEventBus`. Publishers call `IIntegrationEventLogService.SaveEventAsync` on the service `DbContext`, inside the business transaction. The inbox matches that.

`IntegrationEventInboxEntry` is a row in `IntegrationEventInbox`.

- `EventId`, primary key, copied from `IntegrationEvent.Id`
- `EventTypeName`
- `CreationTime`, copied from the event
- `ConsumedTime`, set when the row is created

`IIntegrationEventInbox` is generic over the service `DbContext`, same as `IntegrationEventLogService<TContext>`.

- `TryEnlistAsync` returns false when the id is already stored. Otherwise it tracks a new row and does not save.
- `SaveEnlistedAsync` runs the caller's save. A unique-key `DbUpdateException` means the other delivery committed first. The method returns false and does not rethrow.
- `TrySaveAsync` enlists and saves immediately. Webhooks use this because the HTTP call cannot join the database transaction.
- `RemoveAsync` deletes the row when the webhook send throws, so a second copy of the message can try the POST again.

Catalog stock and the catalog outbox write use `TryEnlistAsync`, then the existing `SaveChanges`. One save commits the stock change, or the outgoing event, and the inbox row. A crash before that save leaves no row, so a redelivery runs the work. A crash after that save leaves the row, so a redelivery returns. That is the crash behavior we want for database side effects.

A decorator on `RabbitMQEventBus` that saved the inbox row before `Handle` would commit the row even when stock later failed to save. The next delivery would skip the stock update. The explicit enlist matches the outbox and keeps the row in the handler's save. Ordering already does command idempotency with `ClientRequest` and `RequestManager`. That store keys client request ids for `IdentifiedCommand`. Integration event handlers call the plain command handlers, so that store does not see these deliveries. Reusing it would mix two ids. The inbox is the event-id store.

Services register `IntegrationEventInbox<TContext>` as transient, next to `IntegrationEventLogService<TContext>`. `UseIntegrationEventInbox` maps the table the way `UseIntegrationEventLogs` maps `IntegrationEventLog`. Catalog and Ordering already call `UseIntegrationEventLogs`. Webhooks gets the inbox map only. Webhooks does not publish through the outbox.

Ordering's `TransactionBehavior` starts a transaction around the cancel command. The handler enlists the inbox row on `OrderingContext` before `mediator.Send`. `SaveEntitiesAsync` then saves the order, the cancel integration event, and the inbox row in that transaction.

Migrations add `IntegrationEventInbox` to Catalog, Ordering (`ordering` schema, same as `IntegrationEventLog`), and Webhooks. No new production NuGet package. Tests use Postgres, the same engine as the services, so the primary key, the ordering schema, and the catalog vector column are real. The EF Core in-memory provider does not enforce a unique key, so it is not the proof.

## Tests

One new test project, `tests/IntegrationEventLogEF.UnitTests`.

- Inbox service. Save the same event id twice. The second save reports a duplicate and the table has one row.
- Catalog paid handler. Stock starts at a known count. `Handle` twice. `AvailableStock` drops by the order quantity once.
- Catalog awaiting-validation handler. `Handle` twice. `IEventBus.PublishAsync` runs once.
- Each migrated webhook handler. `Handle` twice. `IWebhooksSender.SendAll` runs once.
- Ordering payment-failed handler. `Handle` twice on an order that can be cancelled. One `OrderStatusChangedToCancelledIntegrationEvent` is written.

Run that project and `tests/Ordering.UnitTests`. Catalog functional tests stay on the existing Aspire fixture. Run them if the catalog model change loads under that fixture. Do not treat a compile as the proof. The double-delivery tests are the proof.

## Residual risk

- PaymentProcessor can still emit two payment events for one stock-confirmed delivery. `SetPaidStatus` drops the second when the first has committed. A concurrent pair can both observe `StockConfirmed`.
- Webhooks write the inbox row before the POST. A crash after the insert and before the POST drops the notification. A crash after a successful POST and before ack does not send twice. The retry storm in INC-001 is the second case.
- `GracePeriodManagerService` publishes a new `GracePeriodConfirmedIntegrationEvent` id on each poll while the order stays `Submitted`. The status guard absorbs a sequential second event. The inbox does not, because the id changed.
- Two deliveries that both pass `TryEnlistAsync` before either commits are settled by the primary key. One save wins. The other returns without applying its write.
