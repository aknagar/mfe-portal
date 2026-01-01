# Multi-Environment Deployment Guide

This guide explains how to deploy the MfePortal to two separate Azure environments: **Test** and **Production**.

## Environment Configuration

### Test Environment (`mfe-portal-test`)
- **Purpose**: Testing and validation
- **Redis SKU**: Basic (minimal cost)
- **Container Resources**: 0.5 vCPU, 1 GiB memory
- **Replicas**: 1 (auto-scaled)
- **Expected Cost**: ~$25-30/month
- **Parameter File**: `parameters.test.json`

### Production Environment (`mfe-portal-prod`)
- **Purpose**: Live application serving users
- **Redis SKU**: Standard (better performance and reliability)
- **Container Resources**: 1 vCPU, 2 GiB memory
- **Replicas**: 3 (auto-scaled up to 10)
- **Expected Cost**: ~$75-100/month
- **Parameter File**: `parameters.prod.json`

## Deployment Architecture

```
┌─────────────────────────────────────────────────────────┐
│                   GitHub Repository                     │
│                  (azure-deploy branch)                  │
└────────────────────┬────────────────────────────────────┘
                     │
        ┌────────────┴────────────┐
        │                         │
        ▼                         ▼
   ┌─────────────┐           ┌─────────────┐
   │     TEST    │           │     PROD    │
   │ Environment │           │ Environment │
   └──────┬──────┘           └──────┬──────┘
          │                         │
   ┌──────▼──────────┐      ┌──────▼──────────┐
   │ Resource Group: │      │ Resource Group: │
   │ rg-mfe-test     │      │ rg-mfe-prod     │
   │                │      │                 │
   │ - Container    │      │ - Container     │
   │   Apps (1 rep) │      │   Apps (3 rep)  │
   │ - Redis Basic  │      │ - Redis Std     │
   │ - Container    │      │ - Container     │
   │   Registry     │      │   Registry      │
   │ - Logs         │      │ - Logs          │
   └────────────────┘      └─────────────────┘
```

## Step-by-Step Deployment

### Prerequisites

```bash
# Install Azure Developer CLI
# Windows: winget install Microsoft.AzureDeveloperCLI
# macOS: brew tap azure/tap && brew install azure-dev

# Verify installation
azd version

# Authenticate
azd auth login
```

### Deploy to TEST Environment

**1. Create Test Environment**

```bash
cd backend

azd env new
# When prompted:
# - Environment name: test
# - Select subscription
# - Region: eastus (or your preferred region)
```

**2. Configure Test Environment**

```bash
# Set Azure environment name
azd env set AZURE_ENV_NAME test

# Optional: Set custom location
azd env set AZURE_LOCATION eastus
```

**3. Provision Test Infrastructure**

```bash
# This creates all Azure resources using parameters.test.json
azd provision
```

**4. Deploy to Test**

```bash
# Build and push container image, deploy to test environment
azd deploy
```

**5. Verify Test Deployment**

```bash
# Get test environment endpoint
azd env list

# Or use Azure CLI to list resources
az containerapp list --resource-group rg-mfe-test

# Check container app details
az containerapp show --name augmentservice-test --resource-group rg-mfe-test
```

### Deploy to PRODUCTION Environment

**1. Create Production Environment**

```bash
azd env new
# When prompted:
# - Environment name: prod
# - Select subscription (same as test)
# - Region: eastus (or your preferred region)
```

**2. Configure Production Environment**

```bash
# Set Azure environment name
azd env set AZURE_ENV_NAME prod

# Optional: Set custom location
azd env set AZURE_LOCATION eastus
```

**3. Provision Production Infrastructure**

```bash
# This creates all Azure resources using parameters.prod.json
# With higher resources: Standard Redis, 3 replicas, 1 vCPU per instance
azd provision
```

**4. Deploy to Production**

```bash
azd deploy
```

**5. Verify Production Deployment**

```bash
# Get production endpoint
azd env list

# List production resources
az containerapp list --resource-group rg-mfe-prod

# Check container app
az containerapp show --name augmentservice-prod --resource-group rg-mfe-prod
```

## Switching Between Environments

### Switch to TEST

```bash
azd env select test
```

### Switch to PRODUCTION

```bash
azd env select prod
```

### View Current Environment

```bash
azd env list
```

## Environment Variables

Each environment maintains separate `.env` files in `.azure/` folder:

```
.azure/
├── test/
│   └── .env          # Test environment variables
└── prod/
    └── .env          # Production environment variables
```

View environment-specific variables:

```bash
azd env select test
azd env list

azd env select prod
azd env list
```

## Monitoring and Logs

### View Logs for TEST

```bash
azd env select test

# Get recent logs
azd monitor --from 30m

# Or use Azure CLI
az containerapp logs show \
  --name augmentservice-test \
  --resource-group rg-mfe-test
```

### View Logs for PRODUCTION

```bash
azd env select prod

# Get recent logs
azd monitor --from 30m

# Or use Azure CLI
az containerapp logs show \
  --name augmentservice-prod \
  --resource-group rg-mfe-prod
```

### Access Log Analytics

Both environments have separate Log Analytics workspaces:

```bash
# List workspaces
az monitor log-analytics workspace list --resource-group rg-mfe-test
az monitor log-analytics workspace list --resource-group rg-mfe-prod

# Query logs
az monitor log-analytics query \
  --workspace <test-workspace-id> \
  --analytics-query "ContainerAppConsoleLogs_CL | where TimeGenerated > ago(1h)"
```

## Cleanup

### Remove TEST Environment

```bash
azd env select test

# Delete all test resources
azd down

# When prompted, confirm resource deletion
```

### Remove PRODUCTION Environment

```bash
azd env select prod

# Delete all production resources
azd down

# When prompted, confirm resource deletion
```

## Parameter Customization

### Customize TEST Parameters

Edit `infra/parameters.test.json`:

```json
{
  "parameters": {
    "containerCpus": { "value": "1" },    // Increase resources if needed
    "containerMemory": { "value": "2Gi" },
    "containerReplicas": { "value": 2 }   // More replicas for load testing
  }
}
```

Then redeploy:

```bash
azd env select test
azd provision
azd deploy
```

### Customize PRODUCTION Parameters

Edit `infra/parameters.prod.json`:

```json
{
  "parameters": {
    "containerCpus": { "value": "2" },    // Higher resources for production
    "containerMemory": { "value": "4Gi" },
    "containerReplicas": { "value": 5 }   // More replicas for high availability
  }
}
```

Then redeploy:

```bash
azd env select prod
azd provision
azd deploy
```

## Cost Comparison

### Monthly Estimates (US East region)

**TEST Environment:**
- Container App (1 replica, 0.5 vCPU): $5-10
- Redis Basic: ~$10
- Container Registry: ~$5
- Log Analytics: ~$2
- **Total: ~$25-30/month**

**PRODUCTION Environment:**
- Container App (3 replicas, 1 vCPU): $30-40
- Redis Standard: ~$30
- Container Registry: ~$5
- Log Analytics: ~$5
- **Total: ~$75-100/month**

**Combined Monthly Cost: ~$100-130**

## Best Practices

### 1. Always Test in TEST First
- Deploy new code to TEST first
- Validate functionality, performance, and logs
- Then deploy to PRODUCTION

### 2. Environment Parity
- Keep TEST and PROD as similar as possible (same Docker image, configuration)
- Only scale resources differently

### 3. Monitoring and Alerting
- Set up alerts for both environments
- Monitor error rates, response times, resource usage

### 4. Backup and Disaster Recovery
- Configure Redis persistence for PROD
- Set up geo-replication for Container Registry
- Document recovery procedures

### 5. Security
- Use managed identities for resource access
- Store secrets in Key Vault (not in code)
- Enable diagnostic logging for all resources

### 6. Auto-Scaling
Configure appropriate replica limits:

**TEST:**
```bicep
minReplicas: 1
maxReplicas: 3
```

**PRODUCTION:**
```bicep
minReplicas: 2
maxReplicas: 10
```

## Troubleshooting

### Container App Not Starting

```bash
# Check status
azd env select test
az containerapp show --name augmentservice-test --resource-group rg-mfe-test

# View logs
az containerapp logs show --name augmentservice-test --resource-group rg-mfe-test
```

### Redis Connection Issues

```bash
# Verify Redis is running
az redis show --name redis-test-<token> --resource-group rg-mfe-test

# Get connection details
az redis list-keys --name redis-test-<token> --resource-group rg-mfe-test
```

### Deployment Fails

```bash
# Check Azure CLI login
az account show

# Re-authenticate if needed
azd auth login

# Validate Bicep templates
az bicep build-params --file infra/main.bicep
```

## Next Steps

1. **CI/CD Pipeline**: Set up GitHub Actions to auto-deploy on commit
2. **Custom Domain**: Configure custom domain names for both environments
3. **API Management**: Add Azure API Management for API versioning
4. **Database**: Add Azure SQL Database for persistent data storage
5. **Key Vault**: Integrate Azure Key Vault for secrets management
6. **Application Insights**: Set up custom metrics and alerts

## References

- [Azure Container Apps Documentation](https://learn.microsoft.com/en-us/azure/container-apps/)
- [Azure Developer CLI Environments](https://learn.microsoft.com/en-us/azure/developer/azure-dev-cli/manage-environments)
- [Azure Bicep Best Practices](https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/best-practices)
- [Azure Container Apps Scaling](https://learn.microsoft.com/en-us/azure/container-apps/scale-app)
