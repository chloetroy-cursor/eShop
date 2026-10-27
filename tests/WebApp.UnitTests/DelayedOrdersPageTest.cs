using eShop.WebApp.Components.Pages.Ops;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace eShop.WebApp.UnitTests;

[TestClass]
public class DelayedOrdersPageTest
{
    private const string TwoOrdersJson = """
        {"asOf":"2026-10-27T23:40:00Z","thresholdMinutes":240,"orders":[
          {"orderNumber":12,"date":"2026-10-27T18:02:11Z","status":"Paid","total":42.5,"ageMinutes":337},
          {"orderNumber":13,"date":"2026-10-27T19:00:00Z","status":"Submitted","total":10,"ageMinutes":280}]}
        """;

    [TestMethod]
    public async Task RendersOneBadgePerRowAndThreshold()
    {
        var html = await RenderAsync(TwoOrdersJson, "ops/delayed-orders");

        Assert.AreEqual(2, CountOccurrences(html, "class=\"status delayed\""));
        Assert.Contains("Delayed · 5h 37m", html);
        Assert.Contains("Delayed · 4h 40m", html);
        Assert.Contains("Orders not shipped within 4h 00m", html);
    }

    [TestMethod]
    public async Task StatusQueryFiltersRowsAndMarksActiveLink()
    {
        var html = await RenderAsync(TwoOrdersJson, "ops/delayed-orders?status=submitted");

        Assert.AreEqual(1, CountOccurrences(html, "class=\"status delayed\""));
        Assert.Contains("Delayed · 4h 40m", html);
        Assert.Contains("href=\"ops/delayed-orders?status=Submitted\" class=\"active\"", html);
    }

    [TestMethod]
    public async Task ShowsEmptyState()
    {
        var html = await RenderAsync("""{"asOf":"2026-10-27T23:40:00Z","thresholdMinutes":240,"orders":[]}""", "ops/delayed-orders");

        Assert.Contains("No orders are delayed.", html);
        Assert.DoesNotContain("class=\"status delayed\"", html);
    }

    private static async Task<string> RenderAsync(string json, string relativeUri)
    {
        var services = new ServiceCollection()
            .AddSingleton(StubHttpMessageHandler.CreateService(json, out _))
            .AddSingleton<NavigationManager>(new TestNavigationManager(relativeUri))
            .AddSupplyValueFromQueryProvider()
            .BuildServiceProvider();

        await using var renderer = new HtmlRenderer(services, NullLoggerFactory.Instance);
        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var root = await renderer.RenderComponentAsync<DelayedOrders>();
            await root.QuiescenceTask;
            return root.ToHtmlString();
        });
    }

    private static int CountOccurrences(string html, string value)
        => html.Split(value).Length - 1;

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager(string relativeUri) => Initialize("http://localhost/", "http://localhost/" + relativeUri);
    }
}
