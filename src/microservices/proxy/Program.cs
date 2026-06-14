var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT") ?? "8000";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services.AddHttpClient("proxy", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

var app = builder.Build();

var monolithUrl = Environment.GetEnvironmentVariable("MONOLITH_URL") ?? "http://localhost:8080";
var moviesServiceUrl = Environment.GetEnvironmentVariable("MOVIES_SERVICE_URL") ?? "http://localhost:8081";
var eventsServiceUrl = Environment.GetEnvironmentVariable("EVENTS_SERVICE_URL") ?? "http://localhost:8082";
var gradualMigration = string.Equals(Environment.GetEnvironmentVariable("GRADUAL_MIGRATION"), "true", StringComparison.OrdinalIgnoreCase);
var moviesMigrationPercent = int.TryParse(Environment.GetEnvironmentVariable("MOVIES_MIGRATION_PERCENT"), out var parsedPercent)
    ? Math.Clamp(parsedPercent, 0, 100)
    : 0;

var random = new Random();

app.MapGet("/health", () => Results.Json(new { status = true }));

app.Map("/{**catchAll}", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var path = context.Request.Path.Value ?? "";
    string targetBaseUrl;

    if (path.StartsWith("/api/movies", StringComparison.OrdinalIgnoreCase))
    {
        // Strangler Fig: gradually shift /api/movies traffic from the monolith
        // to the new movies microservice based on a percentage feature flag.
        targetBaseUrl = gradualMigration && random.Next(100) < moviesMigrationPercent
            ? moviesServiceUrl
            : monolithUrl;
    }
    else if (path.StartsWith("/api/events", StringComparison.OrdinalIgnoreCase))
    {
        targetBaseUrl = eventsServiceUrl;
    }
    else
    {
        targetBaseUrl = monolithUrl;
    }

    await ProxyRequestAsync(context, httpClientFactory.CreateClient("proxy"), targetBaseUrl);
});

app.Run();

static async Task ProxyRequestAsync(HttpContext context, HttpClient client, string targetBaseUrl)
{
    var request = context.Request;
    var targetUri = new Uri(new Uri(targetBaseUrl), request.Path + request.QueryString);

    using var proxyRequest = new HttpRequestMessage(new HttpMethod(request.Method), targetUri);

    if (HttpMethods.IsPost(request.Method) || HttpMethods.IsPut(request.Method) || HttpMethods.IsPatch(request.Method))
    {
        proxyRequest.Content = new StreamContent(request.Body);
        if (!string.IsNullOrEmpty(request.ContentType))
        {
            proxyRequest.Content.Headers.TryAddWithoutValidation("Content-Type", request.ContentType);
        }
    }

    foreach (var header in request.Headers)
    {
        if (string.Equals(header.Key, "Host", StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }
        proxyRequest.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
    }

    using var responseMessage = await client.SendAsync(proxyRequest, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);

    context.Response.StatusCode = (int)responseMessage.StatusCode;

    foreach (var header in responseMessage.Headers)
    {
        context.Response.Headers[header.Key] = header.Value.ToArray();
    }
    foreach (var header in responseMessage.Content.Headers)
    {
        context.Response.Headers[header.Key] = header.Value.ToArray();
    }
    context.Response.Headers.Remove("transfer-encoding");

    await responseMessage.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
}
