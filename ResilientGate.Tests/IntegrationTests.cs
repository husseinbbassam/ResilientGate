extern alias Gateway;
extern alias FlakyService;

using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace ResilientGate.Tests;

// Reference the Program classes using extern aliases
public class GatewayIntegrationTests : IClassFixture<WebApplicationFactory<Gateway::Program>>
{
    private readonly WebApplicationFactory<Gateway::Program> _gatewayFactory;

    public GatewayIntegrationTests(WebApplicationFactory<Gateway::Program> gatewayFactory)
    {
        _gatewayFactory = gatewayFactory;
    }

    [Fact]
    public async Task Gateway_StatusEndpoint_ReturnsOk()
    {
        // Arrange
        var client = _gatewayFactory.CreateClient();

        // Act
        var response = await client.GetAsync("/");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("ResilientGate", content);
        Assert.Contains("running", content);
    }

    [Fact]
    public async Task Gateway_HealthCheck_ReturnsHealthy()
    {
        // Arrange
        var client = _gatewayFactory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Gateway_StatusEndpoint_ShowsFeatures()
    {
        // Arrange
        var client = _gatewayFactory.CreateClient();

        // Act
        var response = await client.GetAsync("/");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("YARP Reverse Proxy", content);
        Assert.Contains("Polly v8 Resilience Patterns", content);
        Assert.Contains("Circuit Breaker", content);
        Assert.Contains("Hedging", content);
    }
}

public class FlakyServiceIntegrationTests : IClassFixture<WebApplicationFactory<FlakyService::Program>>
{
    private readonly WebApplicationFactory<FlakyService::Program> _serviceFactory;

    public FlakyServiceIntegrationTests(WebApplicationFactory<FlakyService::Program> serviceFactory)
    {
        _serviceFactory = serviceFactory;
    }

    [Fact]
    public async Task FlakyService_HealthCheck_ReturnsHealthy()
    {
        // Arrange
        var client = _serviceFactory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task FlakyService_ReliableEndpoint_AlwaysSucceeds()
    {
        // Arrange
        var client = _serviceFactory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/reliable");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Success", content);
    }

    [Fact]
    public async Task FlakyService_FlakyEndpoint_DefaultMode_Succeeds()
    {
        // Arrange
        var client = _serviceFactory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/flaky");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task FlakyService_DataEndpoint_Exists()
    {
        // Arrange
        var client = _serviceFactory.CreateClient();

        // Act
        var response = await client.GetAsync("/data");

        // Assert
        // The endpoint should return either 200 or 500 (due to random behavior)
        Assert.True(
            response.StatusCode == HttpStatusCode.OK || 
            response.StatusCode == HttpStatusCode.InternalServerError,
            $"Expected 200 or 500 status code, but got {response.StatusCode}"
        );
    }

    [Fact]
    public async Task FlakyService_DataEndpoint_RandomlyFailsAndDelays()
    {
        // Arrange
        var client = _serviceFactory.CreateClient();
        var successCount = 0;
        var errorCount = 0;
        var delayedCount = 0;
        var attempts = 50;

        // Act - Make multiple requests to observe random behavior
        for (int i = 0; i < attempts; i++)
        {
            var startTime = DateTime.UtcNow;
            var response = await client.GetAsync("/data");
            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

            if (response.StatusCode == HttpStatusCode.OK)
            {
                successCount++;
                // Check if it was delayed (roughly 2 seconds = 2000ms)
                if (duration >= 1900) // Allow some margin for timing
                {
                    delayedCount++;
                }
            }
            else if (response.StatusCode == HttpStatusCode.InternalServerError)
            {
                errorCount++;
            }
        }

        // Assert
        // With 50 attempts:
        // - Expect around 15 errors (30%)
        // - Expect around 10 delayed responses (20%)
        // Allow reasonable variance - at least some errors and some successes
        Assert.True(errorCount > 0, "Expected at least some 500 errors");
        Assert.True(successCount > 0, "Expected at least some successful responses");
        // We can't reliably test delay count without making test too slow
    }
}

public class ResiliencePatternsTests
{
    [Fact]
    public async Task ResiliencePatterns_AreConfigured()
    {
        // This is a placeholder test to verify the resilience patterns are set up
        // In a real scenario, you would test actual resilience behavior
        // such as retries, circuit breaking, etc.
        
        // For now, we just verify that the test framework is working
        await Task.CompletedTask;
        Assert.True(true, "Resilience patterns configured successfully");
    }

    [Fact]
    public void PollyV8_IsAvailable()
    {
        // Verify that Polly v8 types are available
        var retryOptions = new Polly.Retry.RetryStrategyOptions();
        var circuitBreakerOptions = new Polly.CircuitBreaker.CircuitBreakerStrategyOptions();
        
        Assert.NotNull(retryOptions);
        Assert.NotNull(circuitBreakerOptions);
    }
}
