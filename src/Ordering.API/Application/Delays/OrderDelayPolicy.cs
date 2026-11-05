using Microsoft.Extensions.Options;

namespace eShop.Ordering.API.Application.Delays;

public sealed class OrderDelayPolicy(IOptions<OrderDelayOptions> options, TimeProvider timeProvider) : IOrderDelayPolicy
{
    public TimeSpan Threshold => options.Value.Threshold;

    public DateTimeOffset GetUtcNow() => timeProvider.GetUtcNow();

    // Npgsql rejects timestamptz parameters whose Kind isn't Utc; UtcDateTime carries Kind = Utc through the subtraction.
    public DateTime GetCutoffUtc(DateTimeOffset now) => now.UtcDateTime - Threshold;

    public bool IsDelayed(OrderStatus status, DateTime orderDateUtc, DateTimeOffset now)
        => OrderDelay.IsDelayed(status, orderDateUtc, now, Threshold);
}
