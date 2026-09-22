namespace eShop.Catalog.API.IntegrationEvents.EventHandling;

public class OrderStatusChangedToPaidIntegrationEventHandler(
    CatalogContext catalogContext,
    IIntegrationEventInbox inbox,
    ILogger<OrderStatusChangedToPaidIntegrationEventHandler> logger) :
    IIntegrationEventHandler<OrderStatusChangedToPaidIntegrationEvent>
{
    public async Task Handle(OrderStatusChangedToPaidIntegrationEvent @event)
    {
        logger.LogInformation("Handling integration event: {IntegrationEventId} - ({@IntegrationEvent})", @event.Id, @event);

        if (!await inbox.TryEnlistAsync(@event))
        {
            logger.LogInformation("Skipping duplicate integration event {IntegrationEventId}", @event.Id);
            return;
        }

        foreach (var orderStockItem in @event.OrderStockItems)
        {
            var catalogItem = catalogContext.CatalogItems.Find(orderStockItem.ProductId);

            catalogItem?.RemoveStock(orderStockItem.Units);
        }

        await inbox.SaveEnlistedAsync(() => catalogContext.SaveChangesAsync());
    }
}
