using eShop.Ordering.Domain.AggregatesModel.OrderAggregate;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace eShop.Ordering.UnitTests.Application;

[TestClass]
public class OrderDelayPolicyTest
{
    private static readonly DateTimeOffset Now = new(2026, 10, 27, 23, 40, 0, TimeSpan.Zero);

    [TestMethod]
    public void Threshold_defaults_to_four_hours_without_config()
    {
        using var provider = BuildProvider(threshold: null);

        Assert.AreEqual(TimeSpan.FromHours(4), provider.GetRequiredService<IOrderDelayPolicy>().Threshold);
    }

    [TestMethod]
    public void Threshold_is_read_from_config()
    {
        using var provider = BuildProvider("00:30:00");

        Assert.AreEqual(TimeSpan.FromMinutes(30), provider.GetRequiredService<IOrderDelayPolicy>().Threshold);
    }

    [TestMethod]
    [DataRow("00:00:00")]
    [DataRow("-01:00:00")]
    [DataRow("00:00:59")]
    public void Threshold_below_one_minute_fails_validation(string threshold)
    {
        using var provider = BuildProvider(threshold);
        var options = provider.GetRequiredService<IOptions<OrderDelayOptions>>();

        var ex = Assert.ThrowsExactly<OptionsValidationException>(() => options.Value);
        Assert.Contains("OrderDelayOptions:Threshold must be at least 1 minute.", ex.Failures);
    }

    [TestMethod]
    public void Threshold_of_exactly_one_minute_is_valid()
    {
        using var provider = BuildProvider("00:01:00");

        Assert.AreEqual(TimeSpan.FromMinutes(1), provider.GetRequiredService<IOptions<OrderDelayOptions>>().Value.Threshold);
    }

    [TestMethod]
    public void GetUtcNow_returns_the_injected_time_provider_value()
    {
        var clock = new TestTimeProvider(Now);
        using var provider = BuildProvider(threshold: null, clock);
        var policy = provider.GetRequiredService<IOrderDelayPolicy>();

        Assert.AreEqual(Now, policy.GetUtcNow());

        clock.UtcNow = Now.AddMinutes(7);
        Assert.AreEqual(Now.AddMinutes(7), policy.GetUtcNow());
    }

    [TestMethod]
    public void GetCutoffUtc_subtracts_threshold_from_now_in_utc()
    {
        using var provider = BuildProvider("00:30:00");
        var now = new DateTimeOffset(2026, 10, 28, 1, 40, 0, TimeSpan.FromHours(2));

        var cutoff = provider.GetRequiredService<IOrderDelayPolicy>().GetCutoffUtc(now);

        Assert.AreEqual(new DateTime(2026, 10, 27, 23, 10, 0, DateTimeKind.Utc), cutoff);
        Assert.AreEqual(now.UtcDateTime - TimeSpan.FromMinutes(30), cutoff);
        Assert.AreEqual(DateTimeKind.Utc, cutoff.Kind);
    }

    [TestMethod]
    public void IsDelayed_uses_configured_threshold_with_inclusive_boundary()
    {
        var clock = new TestTimeProvider(Now);
        using var provider = BuildProvider("00:30:00", clock);
        var policy = provider.GetRequiredService<IOrderDelayPolicy>();
        var orderDate = Now.UtcDateTime;

        clock.UtcNow = Now.AddMinutes(30).AddTicks(-1);
        Assert.IsFalse(policy.IsDelayed(OrderStatus.Submitted, orderDate, policy.GetUtcNow()));

        clock.UtcNow = Now.AddMinutes(30);
        Assert.IsTrue(policy.IsDelayed(OrderStatus.Submitted, orderDate, policy.GetUtcNow()));
        Assert.IsFalse(policy.IsDelayed(OrderStatus.Shipped, orderDate, policy.GetUtcNow()));
    }

    [TestMethod]
    public void Defaults_to_the_system_time_provider()
    {
        using var provider = BuildProvider(threshold: null);

        Assert.AreSame(TimeProvider.System, provider.GetRequiredService<TimeProvider>());
    }

    private static ServiceProvider BuildProvider(string threshold, TimeProvider timeProvider = null)
    {
        var settings = new Dictionary<string, string>();
        if (threshold is not null)
        {
            settings["OrderDelayOptions:Threshold"] = threshold;
        }

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(settings).Build());
        if (timeProvider is not null)
        {
            services.AddSingleton(timeProvider);
        }

        services.AddOrderDelayDetection();
        return services.BuildServiceProvider();
    }
}
