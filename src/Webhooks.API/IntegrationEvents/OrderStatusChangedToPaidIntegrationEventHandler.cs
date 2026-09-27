namespace Webhooks.API.IntegrationEvents;

public class OrderStatusChangedToPaidIntegrationEventHandler(
    IWebhooksRetriever retriever,
    IWebhooksSender sender,
    IIntegrationEventInbox inbox,
    ILogger<OrderStatusChangedToShippedIntegrationEventHandler> logger) : IIntegrationEventHandler<OrderStatusChangedToPaidIntegrationEvent>
{
    public async Task Handle(OrderStatusChangedToPaidIntegrationEvent @event)
    {
        if (!await inbox.TrySaveAsync(@event))
        {
            logger.LogInformation("Skipping duplicate integration event {IntegrationEventId}", @event.Id);
            return;
        }

        try
        {
            var subscriptions = await retriever.GetSubscriptionsOfType(WebhookType.OrderPaid);

            logger.LogInformation("Received OrderStatusChangedToShippedIntegrationEvent and got {SubscriptionsCount} subscriptions to process", subscriptions.Count());

            var whook = new WebhookData(WebhookType.OrderPaid, @event);

            await sender.SendAll(subscriptions, whook);
        }
        catch
        {
            await inbox.RemoveAsync(@event.Id);
            throw;
        }
    }
}
