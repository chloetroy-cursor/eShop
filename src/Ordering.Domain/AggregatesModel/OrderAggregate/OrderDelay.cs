namespace eShop.Ordering.Domain.AggregatesModel.OrderAggregate;

public static class OrderDelay
{
    public static IReadOnlyList<OrderStatus> NotShippedStatuses { get; } =
    [
        OrderStatus.Submitted,
        OrderStatus.AwaitingValidation,
        OrderStatus.StockConfirmed,
        OrderStatus.Paid
    ];

    // The boundary is inclusive to match GracePeriodManagerService. orderDateUtc is treated as UTC
    // whatever its Kind, because converting an Unspecified DateTime to DateTimeOffset would assume local time.
    public static bool IsDelayed(OrderStatus status, DateTime orderDateUtc, DateTimeOffset now, TimeSpan threshold)
        => NotShippedStatuses.Contains(status) && now.UtcDateTime - orderDateUtc >= threshold;
}
