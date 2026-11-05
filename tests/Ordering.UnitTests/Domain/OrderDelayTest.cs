namespace eShop.Ordering.UnitTests.Domain;

using eShop.Ordering.Domain.AggregatesModel.OrderAggregate;

[TestClass]
public class OrderDelayTest
{
    private static readonly TimeSpan Threshold = TimeSpan.FromHours(4);
    private static readonly DateTimeOffset Now = new(2026, 10, 27, 23, 40, 0, TimeSpan.Zero);

    [TestMethod]
    [DataRow(OrderStatus.Submitted, 0L, true)]
    [DataRow(OrderStatus.Submitted, TimeSpan.TicksPerHour, true)]
    [DataRow(OrderStatus.Submitted, -1L, false)]
    [DataRow(OrderStatus.AwaitingValidation, 0L, true)]
    [DataRow(OrderStatus.AwaitingValidation, TimeSpan.TicksPerHour, true)]
    [DataRow(OrderStatus.AwaitingValidation, -1L, false)]
    [DataRow(OrderStatus.StockConfirmed, 0L, true)]
    [DataRow(OrderStatus.StockConfirmed, TimeSpan.TicksPerHour, true)]
    [DataRow(OrderStatus.StockConfirmed, -1L, false)]
    [DataRow(OrderStatus.Paid, 0L, true)]
    [DataRow(OrderStatus.Paid, TimeSpan.TicksPerHour, true)]
    [DataRow(OrderStatus.Paid, -1L, false)]
    public void Not_shipped_order_is_delayed_from_exactly_the_threshold(OrderStatus status, long ticksPastThreshold, bool expected)
    {
        var orderDate = Now.UtcDateTime - Threshold - TimeSpan.FromTicks(ticksPastThreshold);

        Assert.AreEqual(expected, OrderDelay.IsDelayed(status, orderDate, Now, Threshold));
    }

    [TestMethod]
    [DataRow(OrderStatus.Shipped, 0L)]
    [DataRow(OrderStatus.Shipped, TimeSpan.TicksPerHour * 4)]
    [DataRow(OrderStatus.Shipped, TimeSpan.TicksPerDay * 30)]
    [DataRow(OrderStatus.Cancelled, 0L)]
    [DataRow(OrderStatus.Cancelled, TimeSpan.TicksPerHour * 4)]
    [DataRow(OrderStatus.Cancelled, TimeSpan.TicksPerDay * 30)]
    public void Terminal_order_is_never_delayed(OrderStatus status, long ageTicks)
    {
        var orderDate = Now.UtcDateTime - TimeSpan.FromTicks(ageTicks);

        Assert.IsFalse(OrderDelay.IsDelayed(status, orderDate, Now, Threshold));
    }

    [TestMethod]
    public void Not_shipped_statuses_exclude_terminal_statuses()
    {
        CollectionAssert.AreEquivalent(
            new[] { OrderStatus.Submitted, OrderStatus.AwaitingValidation, OrderStatus.StockConfirmed, OrderStatus.Paid },
            OrderDelay.NotShippedStatuses.ToArray());
    }

    [TestMethod]
    public void Age_is_measured_in_utc_when_now_has_an_offset()
    {
        var nowWithOffset = new DateTimeOffset(2026, 10, 28, 1, 40, 0, TimeSpan.FromHours(2));
        var orderDate = new DateTime(2026, 10, 27, 19, 40, 0, DateTimeKind.Utc);

        Assert.IsTrue(OrderDelay.IsDelayed(OrderStatus.Paid, orderDate, nowWithOffset, Threshold));
        Assert.IsFalse(OrderDelay.IsDelayed(OrderStatus.Paid, orderDate.AddTicks(1), nowWithOffset, Threshold));
    }
}
