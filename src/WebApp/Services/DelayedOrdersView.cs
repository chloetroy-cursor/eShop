namespace eShop.WebApp.Services;

public static class DelayedOrdersView
{
    public static IReadOnlyList<string> Statuses { get; } = ["Submitted", "AwaitingValidation", "StockConfirmed", "Paid"];

    public static IReadOnlyList<DelayedOrderRecord> Filter(IEnumerable<DelayedOrderRecord> orders, string? status)
    {
        if (!Statuses.Contains(status, StringComparer.OrdinalIgnoreCase))
        {
            return orders.ToList();
        }

        return orders.Where(o => string.Equals(o.Status, status, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public static string FormatAge(int ageMinutes)
        => ageMinutes < 60 ? $"{ageMinutes}m" : $"{ageMinutes / 60}h {ageMinutes % 60:00}m";
}
