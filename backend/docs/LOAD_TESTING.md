# Load Testing Guide

This guide explains how to perform load and performance testing for the MfePortal solution using k6, integrated with .NET Aspire for orchestration.

## Table of Contents

- [Overview](#overview)
- [Prerequisites](#prerequisites)
- [Test Scripts](#test-scripts)
- [Running Load Tests](#running-load-tests)
- [Test Configuration](#test-configuration)
- [Interpreting Results](#interpreting-results)
- [Best Practices](#best-practices)
- [Advanced Scenarios](#advanced-scenarios)
- [Troubleshooting](#troubleshooting)

## Overview

The MfePortal solution uses [k6](https://k6.io/) for performance and load testing, integrated with .NET Aspire for seamless orchestration. k6 is a modern, developer-centric load testing tool that uses JavaScript for scripting tests.

### Key Features

- **Aspire Integration**: k6 runs as a containerized resource in Aspire, with automatic service discovery
- **Environment Variables**: Aspire automatically injects service endpoints into k6 tests
- **Multiple Test Types**: Smoke tests, load tests, stress tests, and spike tests
- **Performance Thresholds**: Automated pass/fail criteria based on performance metrics
- **Rich Metrics**: Detailed performance metrics including response times, throughput, and error rates

### Architecture

```
┌─────────────────┐
│  Aspire AppHost │
│                 │
│  ┌───────────┐  │      ┌──────────────┐
│  │ k6 Runner │──┼─────►│ AugmentService│
│  └───────────┘  │      └──────────────┘
│                 │
│  ┌───────────┐  │
│  │Test Scripts│  │
│  │(mounted)   │  │
│  └───────────┘  │
└─────────────────┘
```

## Prerequisites

### Local k6 Installation (Optional)

While k6 runs in a container via Aspire, you can also install it locally for standalone testing:

**Windows:**
```powershell
choco install k6
# or
winget install k6 --source winget
```

**macOS:**
```bash
brew install k6
```

**Linux:**
```bash
sudo gpg -k
sudo gpg --no-default-keyring --keyring /usr/share/keyrings/k6-archive-keyring.gpg --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys C5AD17C747E3415A3642D57D77C6C491D6AC1D69
echo "deb [signed-by=/usr/share/keyrings/k6-archive-keyring.gpg] https://dl.k6.io/deb stable main" | sudo tee /etc/apt/sources.list.d/k6.list
sudo apt-get update
sudo apt-get install k6
```

### Docker

Required for running k6 via Aspire. Ensure Docker Desktop is running:

```powershell
docker --version
```

## Test Scripts

All test scripts are located in `backend/tests/k6/scripts/`. Each script is designed for specific testing scenarios.

### 1. smoke-test.js

**Purpose**: Quick sanity check to verify basic functionality  
**Load**: 1 virtual user  
**Duration**: 30 seconds  
**Use Case**: Pre-deployment validation, CI/CD pipelines

**What it tests:**
- Health endpoint availability
- Basic API endpoint accessibility
- Response time under minimal load

**Thresholds:**
- 99% of requests < 1000ms
- Error rate < 5%

### 2. main.js

**Purpose**: Standard load test for health endpoints  
**Load**: 10 virtual users  
**Duration**: 2 minutes  
**Use Case**: Regular performance testing

**Load Profile:**
```
30s: Ramp up to 10 users
1m:  Maintain 10 users
30s: Ramp down to 0 users
```

**Thresholds:**
- 95% of requests < 500ms
- Error rate < 1%

### 3. proxy-test.js

**Purpose**: Load test for proxy/augment API endpoints  
**Load**: 15 virtual users  
**Duration**: 2 minutes  
**Use Case**: Testing data transformation and proxy features

**Load Profile:**
```
30s: Ramp up to 15 users
1m:  Maintain 15 users
30s: Ramp down to 0 users
```

**Thresholds:**
- 95% of requests < 1000ms (allows for proxy latency)
- Error rate < 5%

### 4. user-permissions-test.js

**Purpose**: Load test for user permissions API  
**Load**: 20 virtual users  
**Duration**: 2 minutes  
**Use Case**: Testing authorization and permission checking

**Load Profile:**
```
30s: Ramp up to 20 users
1m:  Maintain 20 users
30s: Ramp down to 0 users
```

**Thresholds:**
- 95% of requests < 500ms
- Error rate < 1%

## Running Load Tests

### Option 1: Via Aspire AppHost (Recommended)

The k6 container is configured in the Aspire AppHost and can be run alongside your services:

1. **Start Aspire AppHost:**

```powershell
cd backend
dotnet run --project MfePortal.AppHost/MfePortal.AppHost.csproj
```

2. **Access Aspire Dashboard:**

Open the Aspire dashboard (typically at `http://localhost:15888` or as shown in console output)

3. **Run k6 Tests:**

The k6 container is configured to run tests automatically when started. You can:
- View test execution in the Aspire dashboard
- Check logs for test results
- Monitor service metrics during test execution

4. **k6 Configuration in AppHost:**

The AppHost configures k6 as follows (from `MfePortal.AppHost/Program.cs`):

```csharp
var k6 = builder.AddK6("k6")
    .WithBindMount("../tests/k6/scripts", "/scripts", isReadOnly: true)
    .WithEnvironment("K6_SCRIPT", "/scripts/main.js")
    .WithReference(augmentService); // Injects service endpoints
```

### Option 2: Standalone k6 (Local Installation)

If you have k6 installed locally, you can run tests directly:

1. **Ensure services are running:**

```powershell
cd backend
dotnet run --project MfePortal.AppHost/MfePortal.AppHost.csproj
```

2. **Set service URL manually:**

```powershell
$env:services__augmentservice__http__0 = "http://localhost:5139"
```

3. **Run specific test:**

```powershell
cd backend/tests/k6/scripts
k6 run smoke-test.js
k6 run main.js
k6 run proxy-test.js
k6 run user-permissions-test.js
```

### Option 3: Docker k6 (No Local Installation)

Run k6 tests directly in Docker:

```powershell
cd backend/tests/k6

# Run smoke test
docker run --rm --network="host" `
  -v ${PWD}/scripts:/scripts `
  -e services__augmentservice__http__0=http://localhost:5139 `
  grafana/k6 run /scripts/smoke-test.js

# Run load test
docker run --rm --network="host" `
  -v ${PWD}/scripts:/scripts `
  -e services__augmentservice__http__0=http://localhost:5139 `
  grafana/k6 run /scripts/main.js
```

## Test Configuration

### Load Test Stages

k6 tests use stages to define load patterns:

```javascript
export const options = {
    stages: [
        { duration: '30s', target: 10 },  // Ramp up
        { duration: '1m', target: 10 },   // Sustain
        { duration: '30s', target: 0 },   // Ramp down
    ],
};
```

### Performance Thresholds

Thresholds define pass/fail criteria:

```javascript
export const options = {
    thresholds: {
        // 95th percentile response time must be < 500ms
        http_req_duration: ['p(95)<500'],
        
        // Error rate must be < 1%
        http_req_failed: ['rate<0.01'],
        
        // 99th percentile must be < 1000ms
        http_req_duration: ['p(99)<1000'],
    },
};
```

### Common Threshold Metrics

| Metric | Description | Example |
|--------|-------------|---------|
| `http_req_duration` | Request duration | `p(95)<500` |
| `http_req_failed` | Failed requests rate | `rate<0.01` |
| `http_reqs` | Requests per second | `rate>100` |
| `iteration_duration` | Full iteration time | `p(95)<1000` |
| `checks` | Check success rate | `rate>0.95` |

### Environment Variables

The Aspire integration automatically injects service endpoints:

```javascript
// Automatically set by Aspire
const baseUrl = __ENV.services__augmentservice__http__0;

// Convention: services__{resourceName}__{bindingName}__{index}
// Example values:
// services__augmentservice__http__0 = "http://augmentservice:8080"
// services__postgres__http__0 = "http://postgres:5432"
```

## Interpreting Results

### Console Output

When a test completes, k6 displays a summary:

```
     ✓ health check status is 200
     ✓ health check response time < 200ms

     checks.........................: 100.00% ✓ 600      ✗ 0   
     data_received..................: 120 kB  2.0 kB/s
     data_sent......................: 30 kB   500 B/s
     http_req_blocked...............: avg=1.2ms   min=0s     med=0s      max=100ms   p(90)=2ms    p(95)=3ms   
     http_req_connecting............: avg=800µs   min=0s     med=0s      max=50ms    p(90)=1.5ms  p(95)=2ms   
     http_req_duration..............: avg=125ms   min=50ms   med=120ms   max=300ms   p(90)=200ms  p(95)=250ms 
       { expected_response:true }...: avg=125ms   min=50ms   med=120ms   max=300ms   p(90)=200ms  p(95)=250ms 
     http_req_failed................: 0.00%   ✓ 0        ✗ 600
     http_req_receiving.............: avg=2ms     min=0s     med=1ms     max=20ms    p(90)=5ms    p(95)=8ms   
     http_req_sending...............: avg=500µs   min=0s     med=0s      max=5ms     p(90)=1ms    p(95)=2ms   
     http_req_tls_handshaking.......: avg=0s      min=0s     med=0s      max=0s      p(90)=0s     p(95)=0s    
     http_req_waiting...............: avg=122.5ms min=49ms   med=119ms   max=295ms   p(90)=195ms  p(95)=245ms 
     http_reqs......................: 600     10/s
     iteration_duration.............: avg=1.12s   min=1.05s  med=1.12s   max=1.3s    p(90)=1.2s   p(95)=1.25s 
     iterations.....................: 600     10/s
     vus............................: 10      min=10     max=10
     vus_max........................: 10      min=10     max=10
```

### Key Metrics Explained

| Metric | What to Look For |
|--------|------------------|
| **checks** | Should be close to 100% - indicates successful validations |
| **http_req_duration (p95)** | 95% of requests faster than this - your SLA target |
| **http_req_failed** | Should be near 0% - indicates error rate |
| **http_reqs** | Requests per second - throughput metric |
| **iteration_duration** | Time for complete test iteration including sleeps |
| **http_req_waiting** | Server processing time (excludes network overhead) |

### Performance Targets

| Service Type | p95 Target | p99 Target | Error Rate |
|--------------|------------|------------|------------|
| Health Checks | < 200ms | < 500ms | < 0.1% |
| Simple APIs | < 500ms | < 1000ms | < 1% |
| Proxy/External | < 1000ms | < 2000ms | < 5% |
| Complex Operations | < 2000ms | < 5000ms | < 5% |

## Best Practices

### 1. Test Incrementally

Start with smoke tests, then gradually increase load:

```javascript
// Smoke test: 1 user
// Load test: 10-50 users
// Stress test: 100+ users
// Spike test: Sudden jumps in load
```

### 2. Use Realistic Data

Generate realistic test data:

```javascript
import { SharedArray } from 'k6/data';

const testData = new SharedArray('test-data', function() {
    return JSON.parse(open('./test-data.json'));
});

export default function() {
    const data = testData[Math.floor(Math.random() * testData.length)];
    // Use data in requests
}
```

### 3. Add Think Time

Simulate realistic user behavior with sleep:

```javascript
import { sleep } from 'k6';

export default function() {
    http.get('...');
    sleep(1); // 1 second think time
    http.post('...', payload);
    sleep(Math.random() * 3); // Random 0-3 seconds
}
```

### 4. Monitor During Tests

While tests run, monitor:
- CPU and memory usage
- Database connections
- Response times in real-time
- Error logs

### 5. Baseline and Compare

Establish baselines and track over time:

```powershell
# Save results
k6 run --out json=results.json main.js

# Compare with baseline
k6 run --out json=results-new.json main.js
# Use tools to compare results.json vs results-new.json
```

## Advanced Scenarios

### Stress Testing

Test system limits by gradually increasing load:

```javascript
export const options = {
    stages: [
        { duration: '2m', target: 100 },  // Ramp to 100 users
        { duration: '5m', target: 100 },  // Stay at 100
        { duration: '2m', target: 200 },  // Ramp to 200
        { duration: '5m', target: 200 },  // Stay at 200
        { duration: '2m', target: 300 },  // Ramp to 300
        { duration: '5m', target: 300 },  // Stay at 300
        { duration: '10m', target: 0 },   // Ramp down
    ],
};
```

### Spike Testing

Test system behavior under sudden load spikes:

```javascript
export const options = {
    stages: [
        { duration: '10s', target: 10 },   // Normal load
        { duration: '1m', target: 10 },    // Stay normal
        { duration: '10s', target: 200 },  // Spike!
        { duration: '3m', target: 200 },   // Stay spiked
        { duration: '10s', target: 10 },   // Back to normal
        { duration: '1m', target: 10 },    // Stay normal
        { duration: '10s', target: 0 },    // Ramp down
    ],
};
```

### Soak Testing

Test system stability over extended periods:

```javascript
export const options = {
    stages: [
        { duration: '5m', target: 50 },   // Ramp up
        { duration: '4h', target: 50 },   // Soak for 4 hours
        { duration: '5m', target: 0 },    // Ramp down
    ],
};
```

### Custom Metrics

Track custom business metrics:

```javascript
import { Trend, Counter } from 'k6/metrics';

const myTrend = new Trend('custom_waiting_time');
const myCounter = new Counter('custom_operation_count');

export default function() {
    const start = new Date();
    const res = http.get('...');
    const duration = new Date() - start;
    
    myTrend.add(duration);
    myCounter.add(1);
}
```

### Authentication

Test with authentication tokens:

```javascript
import encoding from 'k6/encoding';

const credentials = encoding.b64encode('username:password');

export default function() {
    const params = {
        headers: {
            'Authorization': `Basic ${credentials}`,
            'Content-Type': 'application/json',
        },
    };
    
    http.get('...', params);
}
```

### Distributed Testing

Run tests across multiple machines:

```powershell
# Master node
k6 run --execution-mode=cloud main.js

# Or use k6 Cloud for distributed testing
k6 cloud main.js
```

## Troubleshooting

### Issue: Service Not Found

**Error:**
```
Get "http://localhost:5139/health": dial tcp: lookup localhost: no such host
```

**Solution:**
1. Ensure services are running via Aspire
2. Check the correct port is used
3. Verify environment variable is set:
   ```powershell
   $env:services__augmentservice__http__0
   ```

### Issue: High Error Rate

**Symptoms:**
- `http_req_failed` > 5%
- Many 500/503 errors

**Solution:**
1. Reduce load to find breaking point
2. Check service logs for errors
3. Monitor resources (CPU, memory, DB connections)
4. Add more `sleep()` time between requests
5. Scale services if infrastructure-limited

### Issue: Slow Response Times

**Symptoms:**
- p95 exceeding thresholds
- Increasing response times over test duration

**Solution:**
1. Profile the application code
2. Check database query performance
3. Monitor network latency
4. Review caching strategy
5. Check for memory leaks

### Issue: Docker Network Issues

**Error:**
```
Error: context deadline exceeded
```

**Solution:**
```powershell
# Use host network on Windows
docker run --network="host" ...

# Or use bridge network and service IP
docker inspect augmentservice | grep IPAddress
```

### Issue: k6 Container Not Starting

**Check Aspire logs:**
```powershell
# View container logs in Aspire dashboard
# Or check Docker logs
docker logs <k6-container-id>
```

**Common causes:**
- Missing script files
- Invalid script path in AppHost
- Script syntax errors

### Issue: Insufficient Virtual Users

**Error:**
```
WARN[0001] Insufficient VUs, reached 10000 active VUs limit
```

**Solution:**
Reduce virtual users or use k6 Cloud for higher limits:
```javascript
export const options = {
    stages: [
        { duration: '1m', target: 500 }, // Reduce from 10000
    ],
};
```

## Performance Optimization Tips

### 1. Connection Reuse

Enable HTTP keep-alive:

```javascript
export const options = {
    batch: 10,
    batchPerHost: 5,
};
```

### 2. Reduce Logging

Minimize console.log in production tests:

```javascript
// Only log errors
if (res.status >= 400) {
    console.log(`Error: ${res.status}`);
}
```

### 3. Use Batch Requests

Group multiple requests:

```javascript
import { batch } from 'k6/http';

export default function() {
    const responses = batch([
        ['GET', baseUrl + '/health'],
        ['GET', baseUrl + '/api/users'],
        ['GET', baseUrl + '/api/permissions'],
    ]);
}
```

### 4. Share Data Efficiently

Use SharedArray for read-only data:

```javascript
import { SharedArray } from 'k6/data';

const data = new SharedArray('users', function() {
    return JSON.parse(open('./users.json'));
});
```

## CI/CD Integration

The project includes a comprehensive GitHub Actions workflow for automated load testing.

### Automated Workflow

The [k6-load-tests.yml](../../.github/workflows/k6-load-tests.yml) workflow provides:

**Triggers:**
- **Manual:** Run on-demand via workflow dispatch with customizable parameters
- **Scheduled:** Weekly runs every Sunday at 2 AM UTC
- **Pull Requests:** Smoke tests on k6-related file changes

**Features:**
- Test selection (smoke, main, proxy, user-permissions, or all)
- Load level adjustment (smoke, normal, stress)
- Automatic service startup and health checks
- Test result artifacts and reports
- PR comments with smoke test results

**Running manually:**

1. Go to Actions → K6 Load Tests → Run workflow
2. Select test script (default: all)
3. Choose load level (smoke/normal/stress)
4. Click "Run workflow"

**Example workflow snippet:**

```yaml
name: K6 Load Tests

on:
  workflow_dispatch:
    inputs:
      test_script:
        description: 'Test script to run'
        type: choice
        options: [all, smoke-test.js, main.js, proxy-test.js]
      load_level:
        description: 'Load level'
        type: choice
        options: [smoke, normal, stress]
  schedule:
    - cron: '0 2 * * 0'  # Weekly on Sundays

jobs:
  load-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      
      - name: Start Services
        run: |
          cd backend
          dotnet run --project MfePortal.AppHost/MfePortal.AppHost.csproj &
          sleep 30
      
      - name: Setup k6
        run: |
          sudo apt-get update
          sudo apt-get install k6
      
      - name: Run k6 Load Test
        working-directory: backend/tests/k6/scripts
        env:
          services__augmentservice__http__0: http://localhost:5139
        run: k6 run main.js --out json=../results/results.json
      
      - name: Upload Results
        uses: actions/upload-artifact@v4
        with:
          name: k6-results
          path: backend/tests/k6/results/*.json
```

See the full workflow at [.github/workflows/k6-load-tests.yml](../../.github/workflows/k6-load-tests.yml) for complete implementation.

## Additional Resources

- [k6 Documentation](https://k6.io/docs/)
- [k6 Examples](https://k6.io/docs/examples/)
- [k6 Testing Practical Guide](https://www.mostlylucid.net/blog/k6-testing-practical)
- [Aspire k6 Integration](https://github.com/CommunityToolkit/Aspire)
- [Performance Testing Best Practices](https://k6.io/docs/testing-guides/test-types/)
- [k6 Cloud](https://k6.io/cloud/)

## Next Steps

1. **Establish Baselines**: Run tests to establish performance baselines
2. **Set SLAs**: Define service level agreements based on test results
3. **Automate**: Integrate load tests into CI/CD pipeline
4. **Monitor**: Set up continuous performance monitoring
5. **Scale**: Use results to inform scaling strategies
6. **Optimize**: Identify and fix performance bottlenecks

## Related Documentation

- [Testing Guide](./TESTING.md) - Unit and integration testing
- [Deployment Guide](./DEPLOYMENT_QUICK_START.md) - Production deployment
- [Architecture](./ARCHITECTURE.md) - System architecture overview
- [API Documentation](./API_LIST.md) - API endpoints reference
