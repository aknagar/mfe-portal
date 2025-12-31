# Commands Reference Guide

Complete reference of all commands used for setting up and deploying the MFE Portal to Azure with test and production environments.

## Table of Contents
- [Git Commands](#git-commands)
- [.NET Build Commands](#net-build-commands)
- [Azure Developer CLI (azd) Commands](#azure-developer-cli-azd-commands)
- [Environment Configuration](#environment-configuration)
- [Verification Commands](#verification-commands)

---

## Git Commands

### Branch Management

```bash
# Create and switch to azure-deploy branch
git checkout -b azure-deploy

# List all branches with commit refs
git branch -v

# Show current branch
git branch --show-current
```

### Merging Branches

```bash
# Switch to main branch
git checkout full-stack

# Merge daprize branch into full-stack
git merge daprize

# Push merged changes to remote
git push origin full-stack
```

### Committing Changes

```bash
# Stage multi-environment configuration files
git add backend/infra/parameters.test.json backend/infra/parameters.prod.json backend/infra/ENVIRONMENTS.md backend/DEPLOYMENT_QUICK_START.md

# Commit with descriptive message
git commit -m "feat: add multi-environment deployment configuration for test and prod"

# View recent commits
git log --oneline -10
```

---

## .NET Build Commands

### Building the Solution

```bash
# Clean all build artifacts
dotnet clean -q

# Build the entire solution
dotnet build -q

# Build with specific warning level (minimal)
dotnet build MfePortal.AppHost/MfePortal.AppHost.csproj /p:WarningLevel=2

# Clean and build (combined)
dotnet clean -q ; dotnet build -q
```

### Running the Application

```bash
# Run AppHost (Aspire orchestrator)
dotnet run --project MfePortal.AppHost/MfePortal.AppHost.csproj

# Run AppHost without rebuilding
dotnet run --project MfePortal.AppHost --no-build

# Run from backend directory
cd "e:\Repos\my\github\mfe-portal\backend"
dotnet run --project MfePortal.AppHost

# Run with specific launch profile (HTTPS)
dotnet run --launch-profile https
```

### Verification Commands

```bash
# Count endpoints with OpenAPI enabled
$files = Get-ChildItem -Recurse -Path "AugmentService/AugmentService.Api" -Include "*.cs" | Select-String "WithOpenApi"
"Endpoints with OpenAPI enabled: " + ($files | Measure-Object).Count

# Check build success (view last line)
dotnet build -q 2>&1 | Select-String "succeeded|Error" | Select-Object -Last 1

# Build and show errors only
dotnet build 2>&1 | Select-String "Error|error"
```

---

## Azure Developer CLI (azd) Commands

### Environment Setup

```bash
# Create a new environment (interactive)
azd env new

# List all environments
azd env list

# Select/switch to test environment
azd env select test

# Select/switch to prod environment
azd env select prod

# Get current environment name
azd env show
```

### Deployment Workflow

```bash
# One-time setup (when first using azd)
azd auth login
azd config set core.allow_telemetry true

# Deploy to Test Environment
azd env select test
azd provision              # Provision Azure resources (one-time)
azd deploy                 # Deploy application code

# Deploy to Production Environment
azd env select prod
azd provision              # Provision Azure resources (one-time)
azd deploy                 # Deploy application code

# Monitor deployment progress
azd up                     # Run provision + deploy together

# Tear down environment (cleanup)
azd down                   # Remove all Azure resources
azd env remove             # Remove environment configuration
```

### Environment Monitoring

```bash
# View environment endpoints
azd show

# View logs for current environment
azd logs

# Follow logs (real-time)
azd logs --follow

# View logs for specific resource
azd logs container-app
```

---

## Environment Configuration

### Parameter Files

```bash
# View test environment parameters
cat backend/infra/parameters.test.json

# View production environment parameters
cat backend/infra/parameters.prod.json

# Customize parameter for current environment
azd env set PARAMETER_NAME value
```

### Environment-Specific Variables

```bash
# Set Azure subscription
azd env set AZURE_SUBSCRIPTION_ID "your-subscription-id"

# Set Azure location
azd env set AZURE_LOCATION "eastus"

# Set Azure tenant ID
azd env set AZURE_TENANT_ID "your-tenant-id"

# View all environment variables
azd env show
```

---

## Verification Commands

### Testing API Endpoints

```bash
# Test proxy endpoint (with SSL certificate bypass)
$response = Invoke-WebRequest -Uri "https://localhost:7226/proxy?url=https://httpbin.org/get" -SkipCertificateCheck -ErrorAction Stop
"Response length: " + ($response.Content.Length)
$response.Content | ConvertFrom-Json

# Test product endpoint
Invoke-WebRequest -Uri http://localhost:5116/api/Product -Method GET

# Test Scalar API documentation UI
# Browser: https://localhost:5116/scalar/v1

# Test OpenAPI specification
# Browser: https://localhost:5116/openapi/v1.json
```

### Port and Connectivity Checks

```bash
# Check if ports are in use
netstat -ano | Select-String "7226|5116|7139"

# Check specific port (Windows)
netstat -ano | findstr :7226

# Verify localhost connectivity
Test-Connection -ComputerName localhost -Count 1

# Check DNS resolution
nslookup localhost
```

### Health Checks (Post-Deployment)

```bash
# Get container app status
az containerapp show --name augmentservice-test --resource-group rg-mfe-test --query "properties.runningStatus"

# Get all container apps in resource group
az containerapp list --resource-group rg-mfe-test --output table

# View container app logs
az containerapp logs show --name augmentservice-test --resource-group rg-mfe-test --follow

# Test deployed API endpoint
Invoke-WebRequest -Uri "https://<container-app-url>/scalar/v1" -SkipCertificateCheck
```

---

## Workflow Examples

### Complete Test Environment Deployment

```bash
# 1. Switch to azure-deploy branch
git checkout azure-deploy

# 2. Create test environment
azd env new
# (Choose name: "test")

# 3. Select test environment
azd env select test

# 4. Provision Azure resources (one-time)
azd provision

# 5. Deploy application
azd deploy

# 6. View deployment results
azd show

# 7. Check logs
azd logs
```

### Complete Production Environment Deployment

```bash
# 1. Create production environment
azd env new
# (Choose name: "prod")

# 2. Select production environment
azd env select prod

# 3. Provision Azure resources (one-time)
azd provision

# 4. Deploy application
azd deploy

# 5. Verify deployment
azd show

# 6. Monitor logs
azd logs --follow
```

### Building and Testing Locally

```bash
# 1. Change to backend directory
cd backend

# 2. Clean and build
dotnet clean -q
dotnet build -q

# 3. Run AppHost orchestrator
dotnet run --project MfePortal.AppHost

# 4. In another terminal, test endpoints
# Wait for "listening on" message, then test proxy
Invoke-WebRequest -Uri "https://localhost:7226/proxy?url=https://httpbin.org/get" -SkipCertificateCheck

# 5. Access Scalar UI
# Browser: https://localhost:7226/scalar/v1
```

### Switching Between Environments

```bash
# View current environment
azd env show

# List available environments
azd env list

# Switch to test
azd env select test

# Switch to prod
azd env select prod

# Deploy to different environment without leaving context
azd deploy
```

---

## Useful Aliases (PowerShell)

Add these to your PowerShell profile (`$PROFILE`) for faster commands:

```powershell
# Git aliases
Set-Alias -Name gadd -Value "git add"
Set-Alias -Name gcom -Value "git commit"
Set-Alias -Name gpush -Value "git push"
Set-Alias -Name glog -Value "git log"

# Azure Developer CLI aliases
Set-Alias -Name azdp -Value "azd provision"
Set-Alias -Name azdd -Value "azd deploy"
Set-Alias -Name azdl -Value "azd logs"

# .NET aliases
Set-Alias -Name db -Value "dotnet build"
Set-Alias -Name dc -Value "dotnet clean"
Set-Alias -Name dr -Value "dotnet run"
```

---

## Common Issues and Solutions

### Port Already in Use

```bash
# Find process using port 7226
netstat -ano | findstr :7226

# Kill process (use PID from above)
taskkill /PID <PID> /F

# Or kill all dotnet processes
taskkill /IM dotnet.exe /F
```

### Stale Build Cache

```bash
# Clean build artifacts
dotnet clean

# Remove obj and bin directories manually
Remove-Item -Recurse -Force "AugmentService\obj"
Remove-Item -Recurse -Force "AugmentService\bin"

# Rebuild
dotnet build
```

### Azure CLI Authentication

```bash
# Login to Azure
az login

# Check current subscription
az account show

# List available subscriptions
az account list --output table

# Switch subscription
az account set --subscription "subscription-id"
```

---

## Documentation References

- **Quick Start Guide**: [backend/DEPLOYMENT_QUICK_START.md](DEPLOYMENT_QUICK_START.md)
- **Detailed Deployment Guide**: [backend/infra/ENVIRONMENTS.md](infra/ENVIRONMENTS.md)
- **Original Deployment Guide**: [backend/infra/DEPLOYMENT.md](infra/DEPLOYMENT.md)
- **API Endpoints List**: [backend/docs/API_LIST.md](docs/API_LIST.md)
- **Architecture Overview**: [backend/docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)

---

## Summary

This reference guide covers:
- ✅ Git branch and commit management
- ✅ .NET build and run commands
- ✅ Azure Developer CLI (azd) deployment workflow
- ✅ Environment configuration and switching
- ✅ API endpoint verification
- ✅ Common troubleshooting commands

For detailed deployment procedures, refer to [DEPLOYMENT_QUICK_START.md](DEPLOYMENT_QUICK_START.md) or [infra/ENVIRONMENTS.md](infra/ENVIRONMENTS.md).
