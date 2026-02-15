# AugmentService Load Tests

This project contains load tests for the AugmentService API using k6 and .NET Aspire.

## Prerequisites

- [k6](https://k6.io/docs/get-started/installation/) - Install k6 for your platform
- .NET 10.0 SDK
- Docker (for Aspire to run dependencies)

## Installing k6

### Windows
```powershell
winget install k6
```

### macOS
```bash
brew install k6
```

### Linux
```bash
sudo gpg -k
sudo gpg --no-default-keyring --keyring /usr/share/keyrings/k6-archive-keyring.gpg --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys C5AD17C747E3415A3642D57D77C6C491D6AC1D69
echo "deb [signed-by=/usr/share/keyrings/k6-archive-keyring.gpg] https://dl.k6.io/deb stable main" | sudo tee /etc/apt/sources.list.d/k6.list
sudo apt-get update
sudo apt-get install k6
```

## Running Load Tests

### Run all load tests
```bash
dotnet test
```

### Run specific test category
```bash
dotnet test --filter "Category=LoadTest"
```

### Run with detailed output
```bash
dotnet test --logger "console;verbosity=detailed"
```

## Test Scripts

The project includes the following k6 test scripts:

- **smoke-test.js** - Basic smoke test with 1 virtual user for 30 seconds
- **main.js** - Standard load test ramping up to 10 users over 2 minutes
- **user-permissions-test.js** - Load test for user permissions API (20 users)
- **proxy-test.js** - Load test for proxy/augment API (15 users)

## Test Structure

Each test:
1. Starts the Aspire AppHost programmatically
2. Waits for the AugmentService to be healthy
3. Runs k6 against the live service endpoint
4. Validates performance thresholds (p95 latency, failure rate)

## Performance Thresholds

- **Smoke tests**: 99% of requests < 1s, <5% failure rate
- **Load tests**: 95% of requests < 500ms, <1% failure rate
- **Proxy tests**: 95% of requests < 1s, <5% failure rate

## Skipping Tests

If k6 is not installed, the tests will automatically skip with a helpful message:
```
SKIPPING: K6 is not installed. Install from https://k6.io/docs/get-started/installation/
```

## Adding New Tests

1. Create a new k6 JavaScript file in the `scripts/` directory
2. Add a new test method in `K6LoadTests.cs`
3. Call `GetK6TestPath("scripts/your-test.js")` to reference the script
4. Run the test with `dotnet test`

## Example k6 Script

```javascript
import http from "k6/http";
import { check, sleep } from "k6";

export const options = {
    vus: 10,
    duration: '30s',
    thresholds: {
        http_req_duration: ['p(95)<500'],
        http_req_failed: ['rate<0.01'],
    },
};

export default function () {
    const baseUrl = __ENV.BASE_URL || 'http://localhost:5000';
    const response = http.get(`${baseUrl}/api/endpoint`);
    
    check(response, {
        'status is 200': (r) => r.status === 200,
    });
    
    sleep(1);
}
```

## Troubleshooting

### Tests skip immediately
- Ensure k6 is installed and in your PATH
- Run `k6 version` to verify installation

### Aspire fails to start
- Ensure Docker is running
- Check that ports aren't already in use
- Review test output for specific errors

### k6 tests fail
- Check the k6 output in test results for specific errors
- Verify the API endpoints exist and are accessible
- Adjust thresholds if performance expectations are too strict
