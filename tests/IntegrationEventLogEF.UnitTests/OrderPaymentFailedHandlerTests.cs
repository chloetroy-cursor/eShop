using eShop.EventBus.Abstractions;
using eShop.EventBus.Events;
using eShop.IntegrationEventLogEF;
using eShop.IntegrationEventLogEF.Services;
using eShop.Ordering.API.Application.Behaviors;
using eShop.Ordering.API.Application.Commands;
using eShop.Ordering.API.Application.IntegrationEvents;
using eShop.Ordering.API.Application.IntegrationEvents.EventHandling;
using eShop.Ordering.API.Application.IntegrationEvents.Events;
using eShop.Ordering.Domain.AggregatesModel.BuyerAggregate;
using eShop.Ordering.Domain.AggregatesModel.OrderAggregate;
using eShop.Ordering.Infrastructure;
using eShop.Ordering.Infrastructure.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace eShop.IntegrationEventLogEF.UnitTests;

[TestClass]
public class OrderPaymentFailedHandlerTests
{
    [TestMethod]
    public async Task Payment_failure_cancels_the_order_once()
    {
        var connectionString = await Postgres.CreateDatabaseAsync();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<OrderingContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IBuyerRepository, BuyerRepository>();
        services.AddScoped<IIntegrationEventLogService, OutboxWriter>();
        services.AddScoped<IOrderingIntegrationEventService, OrderingIntegrationEventService>();
        services.AddSingleton<IEventBus, NoOpEventBus>();
        services.AddScoped<IIntegrationEventInbox, IntegrationEventInbox<OrderingContext>>();
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<CancelOrderCommandHandler>();
            cfg.AddOpenBehavior(typeof(TransactionBehavior<,>));
        });

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<OrderingContext>();
        await context.Database.EnsureCreatedAsync();

        var buyer = new Buyer(Guid.NewGuid().ToString("N"), "Ada");
        context.Buyers.Add(buyer);
        await context.SaveChangesAsync();

        var order = new Order(
            "user",
            "Ada",
            new Address("street", "city", "state", "country", "zip"),
            cardTypeId: 1,
            cardNumber: "4111",
            cardSecurityNumber: "123",
            cardHolderName: "Ada",
            cardExpiration: DateTime.UtcNow.AddYears(1),
            buyerId: buyer.Id);
        order.ClearDomainEvents();
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var handler = new OrderPaymentFailedIntegrationEventHandler(
            scope.ServiceProvider.GetRequiredService<IMediator>(),
            scope.ServiceProvider.GetRequiredService<IIntegrationEventInbox>(),
            NullLogger<OrderPaymentFailedIntegrationEventHandler>.Instance);
        var integrationEvent = new OrderPaymentFailedIntegrationEvent(order.Id);

        await handler.Handle(integrationEvent);
        await handler.Handle(integrationEvent);

        var cancelEvents = await context.Set<IntegrationEventLogEntry>()
            .CountAsync(entry => entry.EventTypeName.Contains("OrderStatusChangedToCancelled"));
        var status = await context.Orders.AsNoTracking()
            .Where(stored => stored.Id == order.Id)
            .Select(stored => stored.OrderStatus)
            .SingleAsync();

        Assert.AreEqual(OrderStatus.Cancelled, status);
        Assert.AreEqual(1, cancelEvents);
        Assert.AreEqual(1, await context.Set<IntegrationEventInboxEntry>().CountAsync());
    }

    sealed class NoOpEventBus : IEventBus
    {
        public Task PublishAsync(IntegrationEvent @event) => Task.CompletedTask;
    }

    sealed class OutboxWriter(OrderingContext context) : IIntegrationEventLogService
    {
        public Task SaveEventAsync(IntegrationEvent @event, IDbContextTransaction transaction)
        {
            ArgumentNullException.ThrowIfNull(transaction);
            context.Database.UseTransaction(transaction.GetDbTransaction());
            context.Set<IntegrationEventLogEntry>().Add(new IntegrationEventLogEntry(@event, transaction.TransactionId));
            return context.SaveChangesAsync();
        }

        public Task<IEnumerable<IntegrationEventLogEntry>> RetrieveEventLogsPendingToPublishAsync(Guid transactionId)
            => Task.FromResult<IEnumerable<IntegrationEventLogEntry>>([]);

        public Task MarkEventAsFailedAsync(Guid eventId) => Task.CompletedTask;

        public Task MarkEventAsInProgressAsync(Guid eventId) => Task.CompletedTask;

        public Task MarkEventAsPublishedAsync(Guid eventId) => Task.CompletedTask;
    }
}
