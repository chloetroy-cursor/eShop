namespace eShop.Ordering.UnitTests.Application;

using Microsoft.AspNetCore.Http.HttpResults;
using eShop.Ordering.API.Application.Queries;
using Order = eShop.Ordering.API.Application.Queries.Order;
using NSubstitute.ExceptionExtensions;

[TestClass]
public class OrdersWebApiTest
{
    private readonly IMediator _mediatorMock;
    private readonly IOrderQueries _orderQueriesMock;
    private readonly IIdentityService _identityServiceMock;
    private readonly ILogger<OrderServices> _loggerMock;

    public OrdersWebApiTest()
    {
        _mediatorMock = Substitute.For<IMediator>();
        _orderQueriesMock = Substitute.For<IOrderQueries>();
        _identityServiceMock = Substitute.For<IIdentityService>();
        _loggerMock = Substitute.For<ILogger<OrderServices>>();
    }

    [TestMethod]
    public async Task Cancel_order_with_requestId_success()
    {
        // Arrange
        _mediatorMock.Send(Arg.Any<IdentifiedCommand<CancelOrderCommand, bool>>(), default)
            .Returns(Task.FromResult(true));

        // Act
        var orderServices = new OrderServices(_mediatorMock, _orderQueriesMock, _identityServiceMock, _loggerMock);
        var result = await OrdersApi.CancelOrderAsync(Guid.NewGuid(), new CancelOrderCommand(1), orderServices);

        // Assert
        Assert.IsInstanceOfType<Ok>(result.Result);
    }

    [TestMethod]
    public async Task Cancel_order_bad_request()
    {
        // Arrange
        _mediatorMock.Send(Arg.Any<IdentifiedCommand<CancelOrderCommand, bool>>(), default)
            .Returns(Task.FromResult(true));

        // Act
        var orderServices = new OrderServices(_mediatorMock, _orderQueriesMock, _identityServiceMock, _loggerMock);
        var result = await OrdersApi.CancelOrderAsync(Guid.Empty, new CancelOrderCommand(1), orderServices);

        // Assert
        Assert.IsInstanceOfType<BadRequest<string>>(result.Result);
    }

    [TestMethod]
    public async Task Ship_order_with_requestId_success()
    {
        // Arrange
        _mediatorMock.Send(Arg.Any<IdentifiedCommand<ShipOrderCommand, bool>>(), default)
            .Returns(Task.FromResult(true));

        // Act
        var orderServices = new OrderServices(_mediatorMock, _orderQueriesMock, _identityServiceMock, _loggerMock);
        var result = await OrdersApi.ShipOrderAsync(Guid.NewGuid(), new ShipOrderCommand(1), orderServices);

        // Assert
        Assert.IsInstanceOfType<Ok>(result.Result);

    }

    [TestMethod]
    public async Task Ship_order_bad_request()
    {
        // Arrange
        _mediatorMock.Send(Arg.Any<IdentifiedCommand<CreateOrderCommand, bool>>(), default)
            .Returns(Task.FromResult(true));

        // Act
        var orderServices = new OrderServices(_mediatorMock, _orderQueriesMock, _identityServiceMock, _loggerMock);
        var result = await OrdersApi.ShipOrderAsync(Guid.Empty, new ShipOrderCommand(1), orderServices);

        // Assert
        Assert.IsInstanceOfType<BadRequest<string>>(result.Result);
    }

    [TestMethod]
    public async Task Get_orders_success()
    {
        // Arrange
        var fakeDynamicResult = Enumerable.Empty<OrderSummary>();

        _identityServiceMock.GetUserIdentity()
            .Returns(Guid.NewGuid().ToString());

        _orderQueriesMock.GetOrdersFromUserAsync(Guid.NewGuid().ToString())
            .Returns(Task.FromResult(fakeDynamicResult));

        // Act
        var orderServices = new OrderServices(_mediatorMock, _orderQueriesMock, _identityServiceMock, _loggerMock);
        var result = await OrdersApi.GetOrdersByUserAsync(orderServices);

        // Assert
        Assert.IsInstanceOfType<Ok<IEnumerable<OrderSummary>>>(result);
    }

    [TestMethod]
    public async Task Get_order_success()
    {
        // Arrange
        var fakeOrderId = 123;
        var fakeDynamicResult = new Order();
        _orderQueriesMock.GetOrderAsync(Arg.Any<int>())
            .Returns(Task.FromResult(fakeDynamicResult));

        // Act
        var orderServices = new OrderServices(_mediatorMock, _orderQueriesMock, _identityServiceMock, _loggerMock);
        var result = await OrdersApi.GetOrderAsync(fakeOrderId, orderServices);

        // Assert
        Assert.IsInstanceOfType<Ok<Order>>(result.Result);
        Assert.AreSame(fakeDynamicResult, ((Ok<Order>)result.Result).Value);
    }

    [TestMethod]
    public async Task Get_order_fails()
    {
        // Arrange
        var fakeOrderId = 123;
#pragma warning disable NS5003
        _orderQueriesMock.GetOrderAsync(Arg.Any<int>())
            .Throws(new KeyNotFoundException());
#pragma warning restore NS5003

        // Act
        var orderServices = new OrderServices(_mediatorMock, _orderQueriesMock, _identityServiceMock, _loggerMock);
        var result = await OrdersApi.GetOrderAsync(fakeOrderId, orderServices);

        // Assert
        Assert.IsInstanceOfType<NotFound>(result.Result);
    }

    [TestMethod]
    public async Task Get_cardTypes_success()
    {
        // Arrange
        var fakeDynamicResult = Enumerable.Empty<CardType>();
        _orderQueriesMock.GetCardTypesAsync()
            .Returns(Task.FromResult(fakeDynamicResult));

        // Act
        var result = await OrdersApi.GetCardTypesAsync(_orderQueriesMock);

        // Assert
        Assert.IsInstanceOfType<Ok<IEnumerable<CardType>>>(result);
        Assert.AreSame(fakeDynamicResult, result.Value);
    }

    [TestMethod]
    public async Task Get_delayed_orders_success()
    {
        // Arrange
        var now = new DateTimeOffset(2026, 10, 27, 23, 40, 0, TimeSpan.Zero);
        var cutoff = new DateTime(2026, 10, 27, 19, 40, 0, DateTimeKind.Utc);
        var policy = Substitute.For<IOrderDelayPolicy>();
        policy.GetUtcNow().Returns(now);
        policy.GetCutoffUtc(now).Returns(cutoff);
        policy.Threshold.Returns(TimeSpan.FromHours(4));

        var rows = new[]
        {
            new OrderSummary { OrderNumber = 12, Date = new DateTime(2026, 10, 27, 18, 2, 11, DateTimeKind.Utc), Status = "Paid", Total = 42.5 },
            new OrderSummary { OrderNumber = 13, Date = new DateTime(2026, 10, 27, 19, 39, 30, DateTimeKind.Utc), Status = "Submitted", Total = 10 }
        };
        _orderQueriesMock.GetDelayedOrdersAsync(Arg.Any<DateTime>(), Arg.Any<int>())
            .Returns(Task.FromResult<IEnumerable<OrderSummary>>(rows));

        // Act
        var result = await OrdersApi.GetDelayedOrdersAsync(_orderQueriesMock, policy);

        // Assert
        Assert.IsInstanceOfType<Ok<DelayedOrdersResponse>>(result.Result);
        var response = ((Ok<DelayedOrdersResponse>)result.Result).Value;
        Assert.AreEqual(now, response.AsOf);
        Assert.AreEqual(240, response.ThresholdMinutes);
        CollectionAssert.AreEqual(new[] { 12, 13 }, response.Orders.Select(o => o.OrderNumber).ToArray());
        Assert.AreEqual(new DelayedOrder { OrderNumber = 12, Date = rows[0].Date, Status = "Paid", Total = 42.5, AgeMinutes = 337 }, response.Orders[0]);
        Assert.AreEqual(240, response.Orders[1].AgeMinutes);
        await _orderQueriesMock.Received(1).GetDelayedOrdersAsync(cutoff, 100);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(501)]
    public async Task Get_delayed_orders_rejects_out_of_range_limit(int limit)
    {
        // Arrange
        var policy = Substitute.For<IOrderDelayPolicy>();

        // Act
        var result = await OrdersApi.GetDelayedOrdersAsync(_orderQueriesMock, policy, limit);

        // Assert
        Assert.IsInstanceOfType<BadRequest<string>>(result.Result);
        await _orderQueriesMock.DidNotReceiveWithAnyArgs().GetDelayedOrdersAsync(default, default);
    }
}
