namespace eShop.Ordering.FunctionalTests;

// Shifts the real clock instead of freezing it so framework code that consumes TimeProvider keeps advancing.
public sealed class OffsetTimeProvider : TimeProvider
{
    public TimeSpan Offset { get; set; }

    public override DateTimeOffset GetUtcNow() => System.GetUtcNow() + Offset;
}
