# Quick Deployment Commands

## Initial Setup (One Time)

```bash
# Authenticate with Azure
azd auth login

# Navigate to backend
cd backend

# Create TEST environment
azd env new     # Choose 'test' as name

# Create PROD environment
azd env new     # Choose 'prod' as name
```

## Deploy to TEST

```bash
cd backend
azd env select test
azd provision   # First time only
azd deploy
```

## Deploy to PRODUCTION

```bash
cd backend
azd env select prod
azd provision   # First time only
azd deploy
```

## View Deployed Services

```bash
# Switch to environment
azd env select test    # or 'prod'

# Show endpoint
azd env list

# Or get via Azure CLI
az containerapp list --resource-group rg-mfe-test    # or rg-mfe-prod
```

## View Logs

```bash
azd env select test    # or 'prod'
azd monitor --from 30m
```

## Configuration Summary

| Item | TEST | PROD |
|------|------|------|
| **Replicas** | 1 | 3 |
| **CPU per replica** | 0.5 vCPU | 1 vCPU |
| **Memory per replica** | 1 GiB | 2 GiB |
| **Redis SKU** | Basic | Standard |
| **Est. Cost** | $25-30/mo | $75-100/mo |
| **Resource Group** | `rg-mfe-test` | `rg-mfe-prod` |

## Parameter Files

- **Test**: `infra/parameters.test.json`
- **Prod**: `infra/parameters.prod.json`

Modify these files to adjust resources before provisioning.

## Cleanup

```bash
# Remove TEST
azd env select test
azd down

# Remove PROD
azd env select prod
azd down
```
