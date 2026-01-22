# ResilientGate Project

A high-performance **Reverse Proxy** built with .NET 9 and C# 13, demonstrating advanced **Resilience Patterns** using **YARP** (Yet Another Reverse Proxy) and **Polly v8**.

## Architecture

The project consists of three main components:

1. **ResilientGate.Gateway**: The main reverse proxy using Microsoft YARP
2. **ResilientGate.FlakyService**: A sample backend API with configurable failure modes
3. **ResilientGate.Tests**: Integration tests using WebApplicationFactory

## Features

### ✅ YARP Configuration
- Routes traffic from the Gateway (port 5000) to the FlakyService (port 5001)
- Configured via `appsettings.json` with flexible routing rules
- Pattern: `/api/{**catch-all}` forwards all API requests to the backend

### ✅ Polly v8 Resilience Pipeline

The Gateway implements a comprehensive resilience pipeline with multiple strategies:

#### 1. **Hedging Strategy**
- Sends a second request if the first takes longer than 250ms
- Helps reduce tail latency
- Configurable for idempotent operations
- First successful response wins, other is cancelled

#### 2. **Retry Policy**
- Exponential backoff with jitter
- Up to 3 retry attempts
- Initial delay: 500ms
- Handles transient failures (5xx errors, timeouts)

#### 3. **Circuit Breaker**
- Failure ratio threshold: 50%
- Minimum throughput: 3 requests
- Sampling duration: 10 seconds
- Break duration: 30 seconds
- Prevents cascading failures when backend is down

#### 4. **Rate Limiter**
- Concurrency limit: 100 requests
- Queue limit: 50 requests
- Protects the Gateway from being overwhelmed

#### 5. **Timeout**
- Overall timeout: 30 seconds per request
- Prevents hanging requests

### ✅ Active Health Checks
- YARP performs active health checks every 10 seconds
- Checks the `/health` endpoint of the backend
- Automatically stops routing to unhealthy destinations
- Passive health checks based on transport failure rate
- 30-second reactivation period for failed destinations

### ✅ Custom Response Transformation
- Adds `X-Resilience-Handled-By: ResilientGate` header to all proxied responses
- Configured in YARP transforms

### ✅ Observability
- Integrated with .NET metrics via `AddMetrics()`
- Logs resilience events:
  - Retry attempts
  - Circuit breaker state changes
  - Hedging operations
  - Rate limit rejections
  - Timeouts

## Getting Started

### Prerequisites
- .NET 9 SDK or later
- Your favorite IDE (Visual Studio, VS Code, Rider)
- Docker and Docker Compose (optional, for containerized deployment)

### Building the Solution

```bash
dotnet build
```

### Running the Tests

```bash
dotnet test
```

### Running the Applications

**Option 1: Using Docker Compose (Recommended)**

```bash
docker-compose up --build
```

This will start both services:
- Gateway: http://localhost:5000
- FlakyService: http://localhost:5001

**Option 2: Manual Start**

**Terminal 1 - Start the FlakyService:**
```bash
cd ResilientGate.FlakyService
dotnet run
```
The service will start on http://localhost:5001

**Terminal 2 - Start the Gateway:**
```bash
cd ResilientGate.Gateway
dotnet run
```
The gateway will start on http://localhost:5000

### Testing the System

**1. Visual Circuit Breaker Demonstration:**

Open your browser and navigate to:
```
http://localhost:5000/index.html
```

This interactive page demonstrates:
- Real-time request/response visualization
- Green indicators (✓) for successful requests
- Red indicators (✗) for failed requests
- Live statistics: Total Requests, Success Rate, etc.
- Circuit Breaker behavior in action

Click "Start" to begin sending requests to the `/data` endpoint and watch the resilience patterns work!

**2. Check Gateway Status:**
```bash
curl http://localhost:5000/
```

**3. Check Gateway Health:**
```bash
curl http://localhost:5000/health
```

**4. Test Proxied Request (via Gateway):**
```bash
curl http://localhost:5000/api/reliable
```

**5. Test the /data endpoint:**
```bash
curl http://localhost:5000/data
```

**6. Configure FlakyService Failure Mode:**
```bash
# Set to intermittent mode (30% failure rate)
curl -X POST http://localhost:5001/api/configure \
  -H "Content-Type: application/json" \
  -d '{"mode":"intermittent","rate":0.3}'

# Set to timeout mode
curl -X POST http://localhost:5001/api/configure \
  -H "Content-Type: application/json" \
  -d '{"mode":"timeout"}'

# Set to error mode (always returns 500)
curl -X POST http://localhost:5001/api/configure \
  -H "Content-Type: application/json" \
  -d '{"mode":"error"}'

# Reset to normal mode
curl -X POST http://localhost:5001/api/configure \
  -H "Content-Type: application/json" \
  -d '{"mode":"none"}'
```

**7. Test Resilience Patterns:**

With the FlakyService in error mode, make requests through the Gateway:
```bash
# The Gateway will retry, and eventually open the circuit breaker
for i in {1..10}; do
  curl http://localhost:5000/api/flaky
  echo ""
done
```

## FlakyService Endpoints

- `GET /health` - Health check endpoint
- `GET /api/reliable` - Always succeeds
- `GET /api/flaky` - Behavior depends on configured mode
- `GET /data` - Random failures (30% chance) and delays (20% chance) for testing resilience
- `GET /api/configure` - Get current configuration
- `POST /api/configure` - Set failure mode

### Failure Modes
- `none`: Normal operation (default)
- `intermittent`: Random failures based on rate (default 30%)
- `timeout`: Delays requests by 10 seconds
- `error`: Always returns HTTP 500

## Technical Stack

- **.NET 9**: Latest .NET runtime
- **C# 13**: Latest C# language features
- **YARP 2.3.0**: Microsoft's reverse proxy library
- **Polly v8**: Resilience and transient-fault-handling library
- **xUnit**: Testing framework
- **WebApplicationFactory**: Integration testing

## Project Structure

```
ResilientGate/
├── ResilientGate.Gateway/
│   ├── Program.cs              # Main Gateway with YARP and Polly configuration
│   ├── appsettings.json        # YARP routes and clusters configuration
│   └── ResilientGate.Gateway.csproj
├── ResilientGate.FlakyService/
│   ├── Program.cs              # Sample API with configurable failures
│   ├── appsettings.json        # Service configuration
│   └── ResilientGate.FlakyService.csproj
├── ResilientGate.Tests/
│   ├── IntegrationTests.cs     # Integration tests
│   └── ResilientGate.Tests.csproj
└── ResilientGate.sln
```

## Key Learnings

This project demonstrates:

1. **YARP Configuration**: How to set up routing, health checks, and transforms
2. **Polly v8 Syntax**: The new resilience pipeline builder pattern using AddResilienceHandler
3. **Resilience Strategies**: Combining multiple strategies for robust systems
4. **Health Checks**: Active and passive health monitoring
5. **Observability**: Logging and metrics for resilience events
6. **Testing**: Integration testing with WebApplicationFactory
7. **Docker Deployment**: Multi-service containerized deployment with docker-compose
8. **Visual Demonstration**: Interactive HTML page for real-time resilience pattern visualization

## Features Showcase

### 🎯 Hedging Policy
Configured to send a second request after 250ms delay, ensuring low-latency responses even when the backend is slow.

### 🔄 Retry with Exponential Backoff
Automatically retries failed requests up to 3 times with intelligent backoff and jitter to handle transient failures.

### ⚡ Circuit Breaker
Monitors failure rates and stops sending traffic to unhealthy backends, preventing cascade failures and allowing time for recovery.

### 📊 Visual Demonstration
Interactive HTML page at `/index.html` shows real-time resilience patterns in action with color-coded success/failure indicators.

### 🐳 Docker Support
Complete docker-compose setup for easy deployment and testing of the entire system.

## License

This is a demonstration project for educational purposes.
