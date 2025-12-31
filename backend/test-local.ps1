#!/usr/bin/env pwsh
# Local Testing Guide for MfePortal with Dapr

Write-Host "=== MfePortal Local Testing ===" -ForegroundColor Green
Write-Host "`nThis script demonstrates testing the AugmentService locally" -ForegroundColor Cyan

# Suppress HTTPS certificate warnings for localhost
$PSDefaultParameterValues["Invoke-WebRequest:SkipCertificateCheck"] = $true

# Configuration
$baseUrl = "https://localhost:7139"
$tests = @()

Write-Host "`n[1] Checking if service is running..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$baseUrl/health" -Method GET -ErrorAction Stop
    Write-Host "✓ Service is running on $baseUrl" -ForegroundColor Green
    Write-Host "  Status: $($response.StatusCode)" -ForegroundColor Green
    $tests += @{ Name = "Service Health Check"; Status = "PASS" }
} catch {
    Write-Host "✗ Service is not responding at $baseUrl" -ForegroundColor Red
    Write-Host "  Make sure to start the service first:" -ForegroundColor Yellow
    Write-Host "  cd backend\AugmentService && dotnet run" -ForegroundColor Gray
    exit 1
}

Write-Host "`n[2] Testing /health endpoint..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$baseUrl/health" -Method GET
    Write-Host "✓ Health endpoint working" -ForegroundColor Green
    $health = $response.Content | ConvertFrom-Json
    Write-Host "  Status: $($health.status)" -ForegroundColor Green
    $tests += @{ Name = "Health Endpoint"; Status = "PASS" }
} catch {
    Write-Host "✗ Health endpoint failed: $_" -ForegroundColor Red
    $tests += @{ Name = "Health Endpoint"; Status = "FAIL" }
}

Write-Host "`n[3] Testing /alive endpoint..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$baseUrl/alive" -Method GET
    Write-Host "✓ Liveness probe working" -ForegroundColor Green
    Write-Host "  Status Code: $($response.StatusCode)" -ForegroundColor Green
    $tests += @{ Name = "Liveness Probe"; Status = "PASS" }
} catch {
    Write-Host "✗ Liveness probe failed: $_" -ForegroundColor Red
    $tests += @{ Name = "Liveness Probe"; Status = "FAIL" }
}

Write-Host "`n[4] Testing /swagger endpoint..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$baseUrl/swagger" -Method GET
    Write-Host "✓ Swagger UI is available" -ForegroundColor Green
    Write-Host "  Access it at: $baseUrl/swagger" -ForegroundColor Cyan
    $tests += @{ Name = "Swagger UI"; Status = "PASS" }
} catch {
    Write-Host "✗ Swagger UI not available: $_" -ForegroundColor Red
    $tests += @{ Name = "Swagger UI"; Status = "FAIL" }
}

Write-Host "`n[5] Testing /proxy endpoint..." -ForegroundColor Yellow
try {
    $url = "https://httpbin.org/status/200"
    $response = Invoke-WebRequest -Uri "$baseUrl/proxy?url=$([System.Web.HttpUtility]::UrlEncode($url))" -Method GET
    Write-Host "✓ Proxy endpoint working" -ForegroundColor Green
    Write-Host "  Status: $($response.StatusCode)" -ForegroundColor Green
    $tests += @{ Name = "Proxy Endpoint"; Status = "PASS" }
} catch {
    Write-Host "✗ Proxy endpoint failed: $_" -ForegroundColor Red
    $tests += @{ Name = "Proxy Endpoint"; Status = "FAIL" }
}

Write-Host "`n[6] Testing /openapi/v1.json endpoint..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$baseUrl/openapi/v1.json" -Method GET
    Write-Host "✓ OpenAPI specification available" -ForegroundColor Green
    $spec = $response.Content | ConvertFrom-Json
    Write-Host "  Version: $($spec.info.version)" -ForegroundColor Green
    Write-Host "  Title: $($spec.info.title)" -ForegroundColor Green
    $tests += @{ Name = "OpenAPI Spec"; Status = "PASS" }
} catch {
    Write-Host "✗ OpenAPI spec not available: $_" -ForegroundColor Red
    $tests += @{ Name = "OpenAPI Spec"; Status = "FAIL" }
}

# Summary
Write-Host "`n=== Test Summary ===" -ForegroundColor Green
$passed = ($tests | Where-Object { $_.Status -eq "PASS" }).Count
$failed = ($tests | Where-Object { $_.Status -eq "FAIL" }).Count
$total = $tests.Count

Write-Host "`nResults:" -ForegroundColor Cyan
$tests | Format-Table -Property Name, Status @{ Expression = { $_.Status }; FormatString = @{ PASS = 'Green'; FAIL = 'Red' } }

Write-Host "`nTotal: $passed/$total tests passed" -ForegroundColor Green

if ($failed -eq 0) {
    Write-Host "✓ All tests passed!" -ForegroundColor Green
    Write-Host "`nYour AugmentService is running correctly." -ForegroundColor Green
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "  1. Access Swagger UI: $baseUrl/swagger" -ForegroundColor Gray
    Write-Host "  2. Test the /proxy endpoint with external URLs" -ForegroundColor Gray
    Write-Host "  3. Check health via /health and /alive endpoints" -ForegroundColor Gray
} else {
    Write-Host "✗ Some tests failed. Please review the output above." -ForegroundColor Red
}

Write-Host "`nDapr Integration Notes:" -ForegroundColor Cyan
Write-Host "  To use Dapr features (state store, pub/sub):" -ForegroundColor Gray
Write-Host "  1. Start Redis: docker run -d --name dapr-redis -p 6379:6379 redis:7-alpine" -ForegroundColor Gray
Write-Host "  2. Run with Dapr sidecar:" -ForegroundColor Gray
Write-Host "     dapr run --app-id augmentservice --app-port 7139 --dapr-http-port 3500 --components-path ../dapr/components -- dotnet run" -ForegroundColor Gray
Write-Host "  3. Access Dapr state store at: http://localhost:3500" -ForegroundColor Gray

Write-Host "`n"
