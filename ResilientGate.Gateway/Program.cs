using System.Net;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.CircuitBreaker;
using Polly.RateLimiting;
using Polly.Retry;
using Polly.Hedging;
using Polly.Timeout;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add metrics for observability
builder.Services.AddMetrics();

// Register ConcurrencyLimiter as a singleton for proper resource management
builder.Services.AddSingleton<ConcurrencyLimiter>(_ => new ConcurrencyLimiter(new ConcurrencyLimiterOptions
{
    PermitLimit = 100,
    QueueLimit = 50
}));

// Configure YARP Reverse Proxy
var proxyBuilder = builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Add health checks for active monitoring
builder.Services.AddHealthChecks();

// Add HttpClient with resilience for YARP forwarder
builder.Services.AddHttpClient("YarpClient")
    .AddResilienceHandler("comprehensive-pipeline", (resiliencePipelineBuilder, context) =>
    {
        var logger = context.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        // 1. Hedging Strategy - Send a second request if the first one takes longer than 200ms
        resiliencePipelineBuilder.AddHedging(new HedgingStrategyOptions<HttpResponseMessage>
        {
            ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                .HandleResult(response => 
                    response.StatusCode == HttpStatusCode.RequestTimeout ||
                    response.StatusCode == HttpStatusCode.TooManyRequests ||
                    response.StatusCode >= HttpStatusCode.InternalServerError)
                .Handle<HttpRequestException>()
                .Handle<TimeoutException>(),
            MaxHedgedAttempts = 1,
            Delay = TimeSpan.FromMilliseconds(200),
            OnHedging = args =>
            {
                logger.LogWarning("Hedging: Sending hedged request due to delay. Attempt: {Attempt}", args.AttemptNumber);
                return default;
            }
        });

        // 2. Retry Policy - With exponential backoff and jitter
        resiliencePipelineBuilder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
        {
            ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                .HandleResult(response => 
                    response.StatusCode == HttpStatusCode.RequestTimeout ||
                    response.StatusCode == HttpStatusCode.TooManyRequests ||
                    response.StatusCode >= HttpStatusCode.InternalServerError)
                .Handle<HttpRequestException>()
                .Handle<TimeoutException>(),
            MaxRetryAttempts = 3,
            BackoffType = DelayBackoffType.Exponential,
            Delay = TimeSpan.FromMilliseconds(500),
            UseJitter = true,
            OnRetry = args =>
            {
                logger.LogWarning("Retry: Attempt {Attempt} after {Duration}ms due to: {Result}", 
                    args.AttemptNumber, 
                    args.RetryDelay.TotalMilliseconds,
                    args.Outcome.Exception?.Message ?? args.Outcome.Result?.StatusCode.ToString());
                return default;
            }
        });

        // 3. Circuit Breaker - Stop sending traffic if the service is down
        resiliencePipelineBuilder.AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
        {
            ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                .HandleResult(response => 
                    response.StatusCode >= HttpStatusCode.InternalServerError)
                .Handle<HttpRequestException>()
                .Handle<TimeoutException>(),
            FailureRatio = 0.5,
            MinimumThroughput = 3,
            SamplingDuration = TimeSpan.FromSeconds(10),
            BreakDuration = TimeSpan.FromSeconds(30),
            OnOpened = args =>
            {
                logger.LogError("Circuit Breaker: Opened due to failures");
                return default;
            },
            OnClosed = args =>
            {
                logger.LogInformation("Circuit Breaker: Closed, service recovered");
                return default;
            },
            OnHalfOpened = args =>
            {
                logger.LogInformation("Circuit Breaker: Half-Opened, testing service");
                return default;
            }
        });

        // 4. Rate Limiter - Prevent the Gateway from being overwhelmed
        var concurrencyLimiter = context.ServiceProvider.GetRequiredService<ConcurrencyLimiter>();
        
        resiliencePipelineBuilder.AddRateLimiter(new RateLimiterStrategyOptions
        {
            RateLimiter = args =>
            {
                return ValueTask.FromResult(concurrencyLimiter.AttemptAcquire());
            },
            OnRejected = args =>
            {
                logger.LogWarning("Rate Limiter: Request rejected due to rate limiting");
                return default;
            }
        });

        // 5. Timeout - Set overall timeout for requests
        resiliencePipelineBuilder.AddTimeout(new TimeoutStrategyOptions
        {
            Timeout = TimeSpan.FromSeconds(30),
            OnTimeout = args =>
            {
                logger.LogWarning("Timeout: Request timed out after {Timeout}s", args.Timeout.TotalSeconds);
                return default;
            }
        });
    });

var app = builder.Build();

// Map reverse proxy
app.MapReverseProxy();

// Health check endpoint
app.MapHealthChecks("/health");

// Simple status endpoint
app.MapGet("/", () => Results.Ok(new 
{ 
    service = "ResilientGate", 
    status = "running",
    timestamp = DateTime.UtcNow,
    features = new[]
    {
        "YARP Reverse Proxy",
        "Polly v8 Resilience Patterns",
        "Active Health Checks",
        "Circuit Breaker",
        "Retry with Exponential Backoff",
        "Rate Limiting",
        "Hedging",
        "Custom Response Headers"
    }
}));

app.Run();

// Make the Program class accessible to tests
public partial class Program { }

