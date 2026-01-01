# Setup GitHub OIDC Workload Identity Federation for Azure
# This script configures Azure to trust GitHub Actions workflows using OIDC
# Requires: Azure CLI, logged in with sufficient permissions

param(
    [Parameter(Mandatory = $true)]
    [string]$GitHubRepo,  # Format: owner/repo (e.g., aknagar/mfe-portal)
    
    [Parameter(Mandatory = $false)]
    [string]$SubscriptionId,
    
    [Parameter(Mandatory = $false)]
    [string]$AppName = "github-actions-mfe-portal"
)

# Get current subscription if not specified
if (-not $SubscriptionId) {
    $SubscriptionId = az account show --query id -o tsv
}

$TenantId = az account show --query tenantId -o tsv
Write-Host "Subscription ID: $SubscriptionId"
Write-Host "Tenant ID: $TenantId"
Write-Host "GitHub Repo: $GitHubRepo"

# Create Azure AD application
Write-Host "Creating Azure AD application for GitHub Actions..."
$AppInfo = az ad app create --display-name $AppName | ConvertFrom-Json
$AppId = $AppInfo.appId
$ObjectId = $AppInfo.id
Write-Host "Created app with AppId: $AppId"

# Create service principal
Write-Host "Creating service principal..."
$SpInfo = az ad sp create --id $AppId | ConvertFrom-Json
$SpObjectId = $SpInfo.id
Write-Host "Created service principal with ObjectId: $SpObjectId"

# Assign Owner role (adjust as needed for your security requirements)
Write-Host "Assigning Owner role to service principal..."
az role assignment create `
    --assignee $SpObjectId `
    --role "Owner" `
    --subscription $SubscriptionId

# Add federated credential for main branch
Write-Host "Adding federated credential for main branch..."
$FederatedCredential = @{
    name     = "github-main"
    issuer   = "https://token.actions.githubusercontent.com"
    subject  = "repo:${GitHubRepo}:ref:refs/heads/main"
    audiences = @("api://AzureADTokenExchange")
} | ConvertTo-Json

az ad app federated-credential create `
    --id $ObjectId `
    --parameters $FederatedCredential

# Add federated credential for all branches
Write-Host "Adding federated credential for all branches..."
$FederatedCredentialAll = @{
    name     = "github-all"
    issuer   = "https://token.actions.githubusercontent.com"
    subject  = "repo:${GitHubRepo}:*"
    audiences = @("api://AzureADTokenExchange")
} | ConvertTo-Json

az ad app federated-credential create `
    --id $ObjectId `
    --parameters $FederatedCredentialAll

# Output the secrets needed for GitHub
Write-Host ""
Write-Host "================================"
Write-Host "Add these secrets to GitHub:"
Write-Host "================================"
Write-Host "AZURE_CLIENT_ID: $AppId"
Write-Host "AZURE_TENANT_ID: $TenantId"
Write-Host "AZURE_SUBSCRIPTION_ID: $SubscriptionId"
Write-Host ""
Write-Host "1. Go to: https://github.com/$GitHubRepo/settings/secrets/actions"
Write-Host "2. Create three new repository secrets with the above values"
