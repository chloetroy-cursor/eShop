namespace eShop.WebApp.UnitTests;

[TestClass]
public class OrderingServiceTest
{
    private const string ExampleJson = """
        {"asOf":"2026-10-27T23:40:00+00:00","thresholdMinutes":240,"orders":[{"orderNumber":12,"date":"2026-10-27T18:02:11Z","status":"Paid","total":42.5,"ageMinutes":337}]}
        """;

    [TestMethod]
    public async Task GetDelayedOrdersRequestsDelayedEndpointAndDeserializesContract()
    {
        var service = StubHttpMessageHandler.CreateService(ExampleJson, out var handler);

        var result = await service.GetDelayedOrders();

        Assert.AreEqual("/api/Orders/delayed?limit=100", handler.RequestUri!.PathAndQuery);
        Assert.AreEqual(new DateTimeOffset(2026, 10, 27, 23, 40, 0, TimeSpan.Zero), result.AsOf);
        Assert.AreEqual(240, result.ThresholdMinutes);
        var order = result.Orders.Single();
        Assert.AreEqual(12, order.OrderNumber);
        Assert.AreEqual(new DateTime(2026, 10, 27, 18, 2, 11, DateTimeKind.Utc), order.Date);
        Assert.AreEqual(DateTimeKind.Utc, order.Date.Kind);
        Assert.AreEqual("Paid", order.Status);
        Assert.AreEqual(42.5m, order.Total);
        Assert.AreEqual(337, order.AgeMinutes);
    }
}
