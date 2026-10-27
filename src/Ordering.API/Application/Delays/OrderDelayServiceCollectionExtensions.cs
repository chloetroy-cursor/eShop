using Microsoft.Extensions.DependencyInjection.Extensions;

namespace eShop.Ordering.API.Application.Delays;

public static class OrderDelayServiceCollectionExtensions
{
    public static IServiceCollection AddOrderDelayDetection(this IServiceCollection services)
    {
        services.AddOptions<OrderDelayOptions>()
            .BindConfiguration(nameof(OrderDelayOptions))
            .Validate(o => o.Threshold >= TimeSpan.FromMinutes(1), "OrderDelayOptions:Threshold must be at least 1 minute.")
            .ValidateOnStart();

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IOrderDelayPolicy, OrderDelayPolicy>();

        return services;
    }
}
