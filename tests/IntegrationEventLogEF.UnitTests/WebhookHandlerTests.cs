using System.Net.Http;
using eShop.EventBus.Abstractions;
using eShop.EventBus.Events;
using eShop.IntegrationEventLogEF;
using eShop.IntegrationEventLogEF.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Webhooks.API.Infrastructure;
using Webhooks.API.IntegrationEvents;
using Webhooks.API.Model;
using Webhooks.API.Services;

namespace eShop.IntegrationEventLogEF.UnitTests;

[TestClass]
public class WebhookHandlerTests
{
    [TestMethod]
    public async Task Paid_webhook_is_sent_once()
    {
        await AssertSentOnceAsync(
            WebhookType.OrderPaid,
            (context, sender) => new OrderStatusChangedToPaidIntegrationEventHandler(
                new WebhooksRetriever(context),
                sender,
                new IntegrationEventInbox<WebhooksContext>(context),
                NullLogger<OrderStatusChangedToShippedIntegrationEventHandler>.Instance),
            new OrderStatusChangedToPaidIntegrationEvent(4, [new OrderStockItem(1, 1)]));
    }

    [TestMethod]
    public async Task Paid_webhook_is_sent_again_after_the_sender_throws()
    {
        var connectionString = await Postgres.CreateDatabaseAsync();
        var options = new DbContextOptionsBuilder<WebhooksContext>()
            .UseNpgsql(connectionString)
            .Options;
        await using var context = new WebhooksContext(options);
        await context.Database.EnsureCreatedAsync();
        context.Subscriptions.Add(new WebhookSubscription
        {
            Type = WebhookType.OrderPaid,
            Date = DateTime.UtcNow,
            DestUrl = "http://127.0.0.1/hook",
            Token = "token",
            UserId = "user"
        });
        await context.SaveChangesAsync();

        var sender = new ThrowOnceSender();
        var handler = new OrderStatusChangedToPaidIntegrationEventHandler(
            new WebhooksRetriever(context),
            sender,
            new IntegrationEventInbox<WebhooksContext>(context),
            NullLogger<OrderStatusChangedToShippedIntegrationEventHandler>.Instance);
        var integrationEvent = new OrderStatusChangedToPaidIntegrationEvent(4, [new OrderStockItem(1, 1)]);

        await Assert.ThrowsExactlyAsync<HttpRequestException>(() => handler.Handle(integrationEvent));
        await handler.Handle(integrationEvent);

        Assert.AreEqual(2, sender.Calls);
        Assert.AreEqual(1, await context.Set<IntegrationEventInboxEntry>().CountAsync());
    }

    [TestMethod]
    public async Task Shipped_webhook_is_sent_once()
    {
        await AssertSentOnceAsync(
            WebhookType.OrderShipped,
            (context, sender) => new OrderStatusChangedToShippedIntegrationEventHandler(
                new WebhooksRetriever(context),
                sender,
                new IntegrationEventInbox<WebhooksContext>(context),
                NullLogger<OrderStatusChangedToShippedIntegrationEventHandler>.Instance),
            new OrderStatusChangedToShippedIntegrationEvent(4, "Shipped", "Ada"));
    }

    static async Task AssertSentOnceAsync(
        WebhookType webhookType,
        Func<WebhooksContext, CountingSender, IIntegrationEventHandler> createHandler,
        IntegrationEvent integrationEvent)
    {
        var connectionString = await Postgres.CreateDatabaseAsync();
        var options = new DbContextOptionsBuilder<WebhooksContext>()
            .UseNpgsql(connectionString)
            .Options;
        await using var context = new WebhooksContext(options);
        await context.Database.EnsureCreatedAsync();
        context.Subscriptions.Add(new WebhookSubscription
        {
            Type = webhookType,
            Date = DateTime.UtcNow,
            DestUrl = "http://127.0.0.1/hook",
            Token = "token",
            UserId = "user"
        });
        await context.SaveChangesAsync();

        var sender = new CountingSender();
        var handler = createHandler(context, sender);

        await handler.Handle(integrationEvent);
        await handler.Handle(integrationEvent);

        Assert.AreEqual(1, sender.Calls);
        Assert.AreEqual(1, await context.Set<IntegrationEventInboxEntry>().CountAsync());
    }

    sealed class CountingSender : IWebhooksSender
    {
        public int Calls { get; private set; }

        public Task SendAll(IEnumerable<WebhookSubscription> receivers, WebhookData data)
        {
            Calls++;
            return Task.CompletedTask;
        }
    }

    sealed class ThrowOnceSender : IWebhooksSender
    {
        public int Calls { get; private set; }

        public Task SendAll(IEnumerable<WebhookSubscription> receivers, WebhookData data)
        {
            Calls++;
            if (Calls == 1)
            {
                throw new HttpRequestException("simulated send failure");
            }

            return Task.CompletedTask;
        }
    }
}
