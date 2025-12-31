# Azure Container Apps Deployment Guide

This guide explains how to deploy the MfePortal solution to Azure Container Apps using Azure Developer CLI (azd) and Bicep Infrastructure as Code.

## Prerequisites

1. **Azure Subscription**: Active Azure subscription
2. **Azure Developer CLI (azd)**: [Install azd](https://learn.microsoft.com/en-us/azure/developer/azure-dev-cli/install-azd)
3. **Azure CLI**: Already included with azd
4. **Docker**: For building and pushing container images
5. **.NET 9.0 SDK**: For building the application

## Architecture

```
┌─────────────────────────────────────────────────────────┐
│                  Azure Container Apps                   │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  ┌──────────────────────────────────────────────────┐  │
│  │  AugmentService Container App                    │  │
│  │  - HTTPS Ingress                                 │  │
│  │  - Dapr Sidecar Enabled                          │  │
│  │  - Environment: Production                       │  │
│  │  - Min Replicas: 1 | Max Replicas: 10          │  │
│  └──────────────────────────────────────────────────┘  │
│                      │                                  │
│                      ▼                                  │
│  ┌──────────────────────────────────────────────────┐  │
│  │  Dapr Components                                 │  │
│  │  - State Store (Redis)                          │  │
│  │  - Pub/Sub (Redis)                              │  │
│  │  - Service Invocation                           │  │
│  └──────────────────────────────────────────────────┘  │
│                      │                                  │
│                      ▼                                  │
│  ┌──────────────────────────────────────────────────┐  │
│  │  Azure Cache for Redis                          │  │
│  │  - SKU: Basic (configurable)                    │  │
│  │  - TLS 1.2 Required                             │  │
│  │  - Managed by Azure                             │  │
│  └──────────────────────────────────────────────────┘  │
│                                                          │
│  ┌──────────────────────────────────────────────────┐  │
│  │  Monitoring & Logging                            │  │
│  │  - Log Analytics Workspace                      │  │
│  │  - Application Insights Integration             │  │
│  │  - Dapr Runtime Logs                            │  │
│  └──────────────────────────────────────────────────┘  │
│                                                          │
└─────────────────────────────────────────────────────────┘
```

## Bicep Files

### `main.bicep`
Creates core Azure resources:
- **Log Analytics Workspace**: For application and Dapr runtime logs
- **Container Registry**: For container image storage
- **Container Apps Environment**: Managed Kubernetes-based container orchestration
- **Azure Cache for Redis**: For Dapr state store and pub/sub

### `container-app.bicep`
Deploys the AugmentService with:
- Container App with Dapr sidecar enabled
- HTTPS ingress configuration
- Environment variables for production
- Auto-scaling rules (1-10 replicas)
- System-assigned managed identity

### `orchestrator.bicep`
Orchestrates deployment by:
- Calling infrastructure module first
- Deploying container app with outputs from infrastructure
- Providing unified outputs for all resources

## Deployment Steps

### Step 1: Install Azure Developer CLI

```bash
# macOS
brew tap azure/tap
brew install azure-dev

# Windows (using winget)
winget install Microsoft.AzureDeveloperCLI

# Or download from: https://aka.ms/azd-install
```

Verify installation:
```bash
azd version
```

### Step 2: Clone and Navigate

```bash
cd backend
# Ensure azure.yaml is in the backend directory
```

### Step 3: Initialize Azure Developer Environment

```bash
azd auth login
```

This opens a browser to authenticate with your Azure account.

### Step 4: Create New Environment

```bash
azd env new
```

Follow prompts:
- **Environment name**: (e.g., `mfe-dev`, `mfe-prod`)
- **Azure subscription**: Select your subscription
- **Azure region**: Select region (e.g., `eastus`, `westus2`)

### Step 5: Set Configuration Parameters

Edit environment variables:
```bash
azd env set AZURE_ENV_NAME mfe-portal-dev
azd env set AZURE_LOCATION eastus
```

View current settings:
```bash
azd env list
```

### Step 6: Provision Azure Resources

```bash
azd provision
```

This will:
1. Validate your Bicep templates
2. Create resource group
3. Deploy infrastructure (Log Analytics, Container Registry, Container Apps Environment, Redis)
4. Output resource endpoints

### Step 7: Build and Deploy

First, ensure the Dockerfile is present and tested locally:

```bash
# From backend directory
docker build -f AugmentService/Dockerfile -t augmentservice:latest .
```

Then deploy:
```bash
azd deploy
```

This will:
1. Build the container image
2. Push to Azure Container Registry
3. Update Container App with new image
4. Deploy with Dapr sidecar enabled

### Step 8: Access Application

Get the deployment URL:
```bash
azd env list | grep AUGMENTSERVICE_ENDPOINT
```

Or check deployed resources:
```bash
az containerapp list --resource-group rg-mfe-<env-name>
az containerapp show --name augmentservice-<token> --resource-group rg-mfe-<env-name>
```

## Dapr Configuration in Azure Container Apps

Dapr is automatically enabled in the container app via Bicep:

```bicep
dapr: {
  enabled: true
  appId: 'augmentservice'
  appPort: 8080
  appProtocol: 'http'
}
```

Dapr components (state store, pub/sub) need to be deployed separately:

```bash
# Deploy Dapr components to Container Apps Environment
az containerapp env dapr-component set \
  --name <cae-name> \
  --resource-group <rg-name> \
  --dapr-component-name statestore \
  --yaml infra/dapr/statestore.yaml
```

## Parameter Customization

Edit `infra/parameters.json` for deployment-specific settings:

```json
{
  "parameters": {
    "redisSku": { "value": "Standard" },
    "redisCapacity": { "value": 1 },
    "containerCpus": { "value": "1" },
    "containerMemory": { "value": "2Gi" },
    "containerReplicas": { "value": 3 }
  }
}
```

## Monitoring and Logs

### View Application Logs

```bash
azd monitor --from 30m
```

### Access Log Analytics

```bash
# Get Log Analytics workspace details
az monitor log-analytics workspace list \
  --resource-group rg-mfe-<env-name>

# Query logs
az monitor log-analytics query \
  --workspace <workspace-id> \
  --analytics-query "ContainerAppConsoleLogs_CL | where TimeGenerated > ago(1h)"
```

### View Dapr Logs

In Azure Portal:
1. Navigate to Container Apps Environment
2. Select your Container App
3. Go to "Logs" > "Console logs"
4. Filter for Dapr runtime messages

## Cleanup

To remove all Azure resources:

```bash
azd down
```

This will delete:
- Container App
- Container Apps Environment
- Container Registry
- Azure Cache for Redis
- Log Analytics Workspace
- Resource Group (optional)

## Troubleshooting

### Container App Not Starting

```bash
# Check container app status
az containerapp show --name augmentservice-<token> --resource-group rg-mfe-<env-name>

# View recent revisions
az containerapp revision list --name augmentservice-<token> --resource-group rg-mfe-<env-name>

# Check container logs
az containerapp logs show --name augmentservice-<token> --resource-group rg-mfe-<env-name>
```

### Redis Connection Issues

```bash
# Verify Redis is running
az redis show --name redis-<token> --resource-group rg-mfe-<env-name>

# Get connection string
az redis list-keys --name redis-<token> --resource-group rg-mfe-<env-name>

# Test connection
redis-cli -h <redis-host>.redis.cache.windows.net -p 6380 -a <key> ping
```

### Dapr Sidecar Issues

Check Container App logs for Dapr errors. Common issues:
- Redis unreachable: Verify network rules
- Component configuration missing: Deploy dapr-component resources
- TLS certificate issues: Ensure TLS 1.2+

## Production Considerations

1. **Scaling**: Adjust `containerReplicas` and max replicas in bicep
2. **Cost Optimization**: Use Standard Redis SKU for production workloads
3. **Security**:
   - Enable managed identity for Redis access
   - Use Key Vault for secrets
   - Restrict Container Registry access
4. **High Availability**: Deploy across availability zones
5. **Disaster Recovery**: Set up geo-replication for Container Registry
6. **Monitoring**: Set up Application Insights and alerting

## Cost Estimation

Approximate monthly costs (US East region):
- **Container App (1 replica, 0.5 vCPU)**: $10-20
- **Azure Cache for Redis (Basic 0GB)**: ~$25
- **Container Registry (Basic)**: ~$5
- **Log Analytics (1GB data)**: ~$5
- **Total**: ~$45-55/month

Costs scale with:
- Number of replicas
- CPU/memory per instance
- Redis tier and size
- Data ingestion to Log Analytics

## Next Steps

1. Set up CI/CD pipeline with GitHub Actions or Azure DevOps
2. Configure custom domains and SSL certificates
3. Implement RBAC and managed identities
4. Set up alerting and monitoring dashboards
5. Plan for multi-region deployment

## References

- [Azure Container Apps Documentation](https://learn.microsoft.com/en-us/azure/container-apps/)
- [Azure Developer CLI Documentation](https://learn.microsoft.com/en-us/azure/developer/azure-dev-cli/)
- [Bicep Documentation](https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/)
- [Dapr on Container Apps](https://learn.microsoft.com/en-us/azure/container-apps/dapr-overview)
- [Azure Cache for Redis](https://learn.microsoft.com/en-us/azure/azure-cache-for-redis/)
