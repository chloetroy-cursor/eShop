namespace eShop.Ordering.API.Application.Delays;

public interface IOrderDelayPolicy
{
    TimeSpan Threshold { get; }

    DateTimeOffset GetUtcNow();

    DateTime GetCutoffUtc(DateTimeOffset now);

    bool IsDelayed(OrderStatus status, DateTime orderDateUtc, DateTimeOffset now);
}
