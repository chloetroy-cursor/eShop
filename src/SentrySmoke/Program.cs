var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseSentry();
var app = builder.Build();
app.UseSentryTracing();
app.MapGet("/boom", () => { throw new InvalidOperationException("sentry smoke"); });
app.Run();
