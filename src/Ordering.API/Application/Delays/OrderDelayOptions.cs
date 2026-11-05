namespace eShop.Ordering.API.Application.Delays;

public class OrderDelayOptions
{
    public TimeSpan Threshold { get; set; } = TimeSpan.FromHours(4);
}
