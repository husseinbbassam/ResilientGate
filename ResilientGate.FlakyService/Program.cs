using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

// Thread-safe configuration storage
builder.Services.AddSingleton(new ConcurrentDictionary<string, object>(
    new Dictionary<string, object>
    {
        ["failureMode"] = "none",
        ["failureRate"] = 0.3
    }));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapHealthChecks("/health");

// Endpoint that always succeeds
app.MapGet("/api/reliable", () =>
{
    return Results.Ok(new { message = "Success", timestamp = DateTime.UtcNow });
})
.WithName("Reliable");

// Endpoint that can be configured to fail
app.MapGet("/api/flaky", async (ConcurrentDictionary<string, object> config) =>
{
    var failureMode = (string)config["failureMode"];
    var failureRate = (double)config["failureRate"];
    
    // Simulate intermittent failures
    if (failureMode == "intermittent" && Random.Shared.NextDouble() < failureRate)
    {
        await Task.Delay(100);
        return Results.StatusCode(500);
    }
    
    // Simulate timeouts
    if (failureMode == "timeout")
    {
        await Task.Delay(TimeSpan.FromSeconds(10));
        return Results.Ok(new { message = "Delayed Success", timestamp = DateTime.UtcNow });
    }
    
    // Simulate errors
    if (failureMode == "error")
    {
        return Results.StatusCode(500);
    }
    
    // Normal success
    return Results.Ok(new { message = "Success", timestamp = DateTime.UtcNow });
})
.WithName("Flaky");

// Endpoint to configure failure mode
app.MapPost("/api/configure", (ConfigureRequest request, ConcurrentDictionary<string, object> config) =>
{
    config["failureMode"] = request.Mode;
    config["failureRate"] = request.Rate ?? 0.3;
    
    var mode = (string)config["failureMode"];
    var rate = (double)config["failureRate"];
    return Results.Ok(new { message = $"Configured to mode: {mode}, rate: {rate}" });
})
.WithName("Configure");

// Endpoint to get current configuration
app.MapGet("/api/configure", (ConcurrentDictionary<string, object> config) =>
{
    return Results.Ok(new { 
        mode = (string)config["failureMode"], 
        rate = (double)config["failureRate"] 
    });
})
.WithName("GetConfiguration");

// Endpoint with random failures and delays for testing retry and hedging policies
app.MapGet("/data", async () =>
{
    const int DelayMs = 2000;
    var random = Random.Shared.NextDouble();
    
    // 30% chance of returning 500 error
    if (random < 0.3)
    {
        return Results.StatusCode(500);
    }
    
    // 20% chance of adding 2-second delay (30% to 50% range)
    if (random < 0.5)
    {
        await Task.Delay(DelayMs);
    }
    
    // Normal success response
    return Results.Ok(new { message = "Success", timestamp = DateTime.UtcNow });
})
.WithName("Data");

app.Run();

record ConfigureRequest(string Mode, double? Rate);

// Make the Program class accessible to tests
public partial class Program { }
