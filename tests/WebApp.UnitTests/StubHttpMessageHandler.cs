using System.Text;

namespace eShop.WebApp.UnitTests;

public sealed class StubHttpMessageHandler(string json) : HttpMessageHandler
{
    public Uri? RequestUri { get; private set; }

    public static OrderingService CreateService(string json, out StubHttpMessageHandler handler)
    {
        handler = new StubHttpMessageHandler(json);
        return new OrderingService(new HttpClient(handler) { BaseAddress = new Uri("http://ordering-api") });
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestUri = request.RequestUri;
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });
    }
}
