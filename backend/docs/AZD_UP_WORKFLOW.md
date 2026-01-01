# `azd up` Workflow

## Quick Overview

`azd up` = `azd provision` (infrastructure) + `azd deploy` (application)

```bash
azd up --environment test --no-prompt
```

## 6-Step Execution Flow

| Step | What | When |
|------|------|------|
| 1 | Read `azure.yaml` + load `.azure/test/.env` | Start |
| 2 | Run `hooks.preprovision` commands | Before infra |
| 3 | Deploy `infra/main.bicep` to Azure | Main provisioning |
| 4 | Run `hooks.postprovision` commands | After infra |
| 5 | Build & push Docker image, update Container App | Deploy |
| 6 | Save outputs to `.azure/test/.env` | Complete |

## Parameter Resolution Chain

```
.azure/test/.env
    ↓
infra/main.parameters.json (variable substitution)
    ↓
main.bicep (receives parameters)
    ↓
Azure resources created
    ↓
Outputs → .azure/test/.env
```

## Key Variables Flow

**.azure/test/.env** provides:
```
AZURE_ENV_NAME=test
AZURE_RESOURCE_GROUP_NAME=rg-mfe-test
AZURE_LOCATION=eastus
AZURE_SUBSCRIPTION_ID=xxx
```

**main.parameters.json** substitutes:
```json
"resourceGroupName": "${AZURE_RESOURCE_GROUP_NAME}"  → rg-mfe-test
"environmentName": "${AZURE_ENV_NAME}"               → test
"location": "${AZURE_LOCATION}"                      → eastus
```

**main.bicep** receives:
```bicep
param resourceGroupName string        // = rg-mfe-test
param environmentName string           // = test
param location string                  // = eastus
```

## What Gets Created

- Resource Group: `rg-mfe-test`
- Container Registry (image storage)
- Container Apps Environment (Dapr-enabled)
- Container App: `augmentservice`
- Log Analytics Workspace
- Application Insights
- Azure Redis Cache
- Key Vault

## Hooks Execution

```yaml
hooks:
  preprovision:
    - run: azd env set ASPNETCORE_ENVIRONMENT Production
  postprovision:
    - run: echo "Resources provisioned successfully"
```

## Service Deployment

For each service in `azure.yaml`:
1. Build Docker image
2. Push to registry
3. Update Container App

## Common Commands

```bash
azd up --environment test              # Full deploy
azd provision                          # Infrastructure only
azd deploy                             # Application only
azd env list                           # View environments
azd env get-values                     # View variables
azd down                               # Destroy resources
```

## Quick Troubleshooting

| Issue | Fix |
|-------|-----|
| Wrong resource group created | Verify `AZURE_RESOURCE_GROUP_NAME` in `.azure/<env>/.env` |
| Old image still running | `azd deploy --no-cache` |
| Parameter type error | Check JSON types match Bicep params |
| Missing env vars in app | Set in Container App or Key Vault |
