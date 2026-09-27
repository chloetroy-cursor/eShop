using eShop.Catalog.API.Infrastructure;
using eShop.Catalog.API.IntegrationEvents;
using eShop.Catalog.API.IntegrationEvents.EventHandling;
using eShop.Catalog.API.IntegrationEvents.Events;
using eShop.Catalog.API.Model;
using eShop.EventBus.Abstractions;
using eShop.EventBus.Events;
using eShop.IntegrationEventLogEF;
using eShop.IntegrationEventLogEF.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Pgvector.EntityFrameworkCore;

namespace eShop.IntegrationEventLogEF.UnitTests;

[TestClass]
public class CatalogHandlerTests
{
    [TestMethod]
    public async Task Paid_event_removes_stock_once()
    {
        var connectionString = await Postgres.CreateDatabaseAsync();
        await using var context = await CreateCatalogContextAsync(connectionString);
        var brand = new CatalogBrand("brand");
        var type = new CatalogType("type");
        context.AddRange(brand, type);
        await context.SaveChangesAsync();

        var item = new CatalogItem("hat")
        {
            CatalogBrandId = brand.Id,
            CatalogTypeId = type.Id,
            AvailableStock = 10,
            MaxStockThreshold = 100,
            Price = 1
        };
        context.CatalogItems.Add(item);
        await context.SaveChangesAsync();

        var handler = new OrderStatusChangedToPaidIntegrationEventHandler(
            context,
            new IntegrationEventInbox<CatalogContext>(context),
            NullLogger<OrderStatusChangedToPaidIntegrationEventHandler>.Instance);
        var integrationEvent = new OrderStatusChangedToPaidIntegrationEvent(1, [new OrderStockItem(item.Id, 3)]);

        await handler.Handle(integrationEvent);
        await handler.Handle(integrationEvent);

        var stock = await context.CatalogItems.AsNoTracking()
            .Where(catalogItem => catalogItem.Id == item.Id)
            .Select(catalogItem => catalogItem.AvailableStock)
            .SingleAsync();

        Assert.AreEqual(7, stock);
        Assert.AreEqual(1, await context.Set<IntegrationEventInboxEntry>().CountAsync());
    }

    [TestMethod]
    public async Task Awaiting_validation_publishes_one_stock_event()
    {
        var connectionString = await Postgres.CreateDatabaseAsync();
        await using var context = await CreateCatalogContextAsync(connectionString);
        var brand = new CatalogBrand("brand");
        var type = new CatalogType("type");
        context.AddRange(brand, type);
        await context.SaveChangesAsync();

        var item = new CatalogItem("hat")
        {
            CatalogBrandId = brand.Id,
            CatalogTypeId = type.Id,
            AvailableStock = 5,
            MaxStockThreshold = 100,
            Price = 1
        };
        context.CatalogItems.Add(item);
        await context.SaveChangesAsync();

        var bus = new CountingEventBus();
        var events = new CatalogIntegrationEventService(
            NullLogger<CatalogIntegrationEventService>.Instance,
            bus,
            context,
            new IntegrationEventLogService<CatalogContext>(context));
        var handler = new OrderStatusChangedToAwaitingValidationIntegrationEventHandler(
            context,
            events,
            new IntegrationEventInbox<CatalogContext>(context),
            NullLogger<OrderStatusChangedToAwaitingValidationIntegrationEventHandler>.Instance);
        var integrationEvent = new OrderStatusChangedToAwaitingValidationIntegrationEvent(
            9,
            [new OrderStockItem(item.Id, 1)]);

        await handler.Handle(integrationEvent);
        await handler.Handle(integrationEvent);

        Assert.AreEqual(1, bus.Publishes);
        Assert.AreEqual(1, await context.Set<IntegrationEventLogEntry>().CountAsync());
        Assert.AreEqual(1, await context.Set<IntegrationEventInboxEntry>().CountAsync());
    }

    static async Task<CatalogContext> CreateCatalogContextAsync(string connectionString)
    {
        var options = new DbContextOptionsBuilder<CatalogContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.UseVector())
            .Options;
        var context = new CatalogContext(options, new ConfigurationBuilder().Build());
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    sealed class CountingEventBus : IEventBus
    {
        public int Publishes { get; private set; }

        public Task PublishAsync(IntegrationEvent @event)
        {
            Publishes++;
            return Task.CompletedTask;
        }
    }
}
