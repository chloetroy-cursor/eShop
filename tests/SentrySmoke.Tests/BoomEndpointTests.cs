using System.IO.Compression;
using Microsoft.AspNetCore.Mvc.Testing;

namespace eShop.SentrySmoke.Tests;

[TestClass]
public class BoomEndpointTests
{
    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    public async Task Boom_throws_sentry_smoke_and_sdk_posts_the_envelope()
    {
        using var listener = new HttpListener();
        var port = ReservePort();
        listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        listener.Start();

        var envelopeTask = ReadEnvelopeAsync(listener);
        var previousDsn = Environment.GetEnvironmentVariable("SENTRY_DSN");
        Environment.SetEnvironmentVariable(
            "SENTRY_DSN",
            $"http://aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa@127.0.0.1:{port}/1");

        try
        {
            await using var factory = new WebApplicationFactory<Program>();
            using var client = factory.CreateClient();

            var response = await client.GetAsync("/boom", TestContext.CancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

            Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode, responseBody);
            Assert.Contains("sentry smoke", responseBody, StringComparison.Ordinal);

            var completed = await Task.WhenAny(envelopeTask, Task.Delay(TimeSpan.FromSeconds(15), TestContext.CancellationToken));
            Assert.AreSame(envelopeTask, completed, "Sentry SDK did not post an envelope");

            var envelope = await envelopeTask;
            Assert.Contains("InvalidOperationException", envelope, StringComparison.Ordinal);
            Assert.Contains("sentry smoke", envelope, StringComparison.Ordinal);
            Assert.Contains("ExceptionHandlerMiddleware", envelope, StringComparison.Ordinal);
        }
        finally
        {
            Environment.SetEnvironmentVariable("SENTRY_DSN", previousDsn);
            listener.Close();
        }
    }

    private static int ReservePort()
    {
        using var tcp = new TcpListener(IPAddress.Loopback, 0);
        tcp.Start();
        return ((IPEndPoint)tcp.LocalEndpoint).Port;
    }

    private static async Task<string> ReadEnvelopeAsync(HttpListener listener)
    {
        var context = await listener.GetContextAsync();
        await using var body = OpenBody(context.Request);
        using var reader = new StreamReader(body);
        var text = await reader.ReadToEndAsync();
        context.Response.StatusCode = (int)HttpStatusCode.OK;
        context.Response.Close();
        return text;
    }

    private static Stream OpenBody(HttpListenerRequest request)
    {
        var encoding = request.Headers["Content-Encoding"];
        if (string.Equals(encoding, "gzip", StringComparison.OrdinalIgnoreCase))
        {
            return new GZipStream(request.InputStream, CompressionMode.Decompress, leaveOpen: true);
        }

        return request.InputStream;
    }
}
