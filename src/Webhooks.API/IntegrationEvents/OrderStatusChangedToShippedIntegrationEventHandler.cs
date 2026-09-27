namespace Webhooks.API.IntegrationEvents;

public class OrderStatusChangedToShippedIntegrationEventHandler(
    IWebhooksRetriever retriever,
    IWebhooksSender sender,
    IIntegrationEventInbox inbox,
    ILogger<OrderStatusChangedToShippedIntegrationEventHandler> logger) : IIntegrationEventHandler<OrderStatusChangedToShippedIntegrationEvent>
{
    public async Task Handle(OrderStatusChangedToShippedIntegrationEvent @event)
    {
        if (!await inbox.TrySaveAsync(@event))
        {
            logger.LogInformation("Skipping duplicate integration event {IntegrationEventId}", @event.Id);
            return;
        }

        try
        {
            var subscriptions = await retriever.GetSubscriptionsOfType(WebhookType.OrderShipped);

            logger.LogInformation("Received OrderStatusChangedToShippedIntegrationEvent and got {SubscriptionCount} subscriptions to process", subscriptions.Count());

            var whook = new WebhookData(WebhookType.OrderShipped, @event);

            await sender.SendAll(subscriptions, whook);
        }
        catch
        {
            await inbox.RemoveAsync(@event.Id);
            throw;
        }
    }
}