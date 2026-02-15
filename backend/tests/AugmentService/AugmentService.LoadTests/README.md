# AugmentService Load Tests

Load testing infrastructure for AugmentService APIs using K6.

## Test Scripts

### 1. User Permissions API (`scripts/user-permissions-test.js`)
Tests the user permissions endpoints:
- `GET /api/permissions` - Get user permissions
- `GET /api/permissions/check?permission=read` - Check specific permission

**Load Profile:**
- Ramp up to 20 users over 30s
- Stay at 20 users for 1m
- Ramp down to 0 over 30s

**Thresholds:**
- 95% of requests < 500ms
- < 1% failure rate

### 2. Proxy API (`scripts/proxy-test.js`)
Tests the proxy/augment endpoints:
- `POST /api/augment` - Augment text
- `GET /api/proxy/health` - Proxy health check

**Load Profile:**
- Ramp up to 15 users over 30s
- Stay at 15 users for 1m
- Ramp down to 0 over 30s

**Thresholds:**
- 95% of requests < 1s
- < 5% failure rate

## Running Load Tests

### Prerequisites
Install K6:
```powershell
# Windows
choco install k6
```

### Run K6 Tests Directly (Recommended)

1. Start the service:
```bash
cd backend
dotnet run --project MfePortal.AppHost
```

2. Run K6 tests:
```bash
cd backend/tests/AugmentService/AugmentService.LoadTests

# Run user permissions test
k6 run scripts/user-permissions-test.js -e BASE_URL=http://localhost:<port>

# Run proxy test
k6 run scripts/proxy-test.js -e BASE_URL=http://localhost:<port>

# Run smoke test
k6 run scripts/smoke-test.js -e BASE_URL=http://localhost:<port>
```

## Test Files

- `scripts/user-permissions-test.js` - User permissions API load test
- `scripts/proxy-test.js` - Proxy API load test
- `scripts/smoke-test.js` - Basic health check
- `K6LoadTests.cs` - xUnit test wrapper (for CI/CD)
