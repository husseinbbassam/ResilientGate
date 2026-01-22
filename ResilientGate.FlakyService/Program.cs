var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapHealthChecks("/health");

// Configuration for failure simulation
var failureMode = "none"; // none, timeout, error, intermittent
var failureRate = 0.3; // For intermittent mode

// Endpoint that always succeeds
app.MapGet("/api/reliable", () =>
{
    return Results.Ok(new { message = "Success", timestamp = DateTime.UtcNow });
})
.WithName("Reliable");

// Endpoint that can be configured to fail
app.MapGet("/api/flaky", async () =>
{
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
app.MapPost("/api/configure", (ConfigureRequest request) =>
{
    failureMode = request.Mode;
    failureRate = request.Rate ?? 0.3;
    return Results.Ok(new { message = $"Configured to mode: {failureMode}, rate: {failureRate}" });
})
.WithName("Configure");

// Endpoint to get current configuration
app.MapGet("/api/configure", () =>
{
    return Results.Ok(new { mode = failureMode, rate = failureRate });
})
.WithName("GetConfiguration");

app.Run();

record ConfigureRequest(string Mode, double? Rate);

// Make the Program class accessible to tests
public partial class Program { }
