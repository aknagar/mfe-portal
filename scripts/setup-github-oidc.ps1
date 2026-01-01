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

# Check if app already exists
$ExistingApp = az ad app list --filter "displayname eq '$AppName'" --query '[0].{id:id, appId:appId}' 2>$null | ConvertFrom-Json

if ($ExistingApp -and $ExistingApp.id) {
    Write-Host "Found existing app, using it..."
    $ObjectId = $ExistingApp.id
    $AppId = $ExistingApp.appId
} else {
    # Create Azure AD application
    Write-Host "Creating Azure AD application for GitHub Actions..."
    $AppInfo = az ad app create --display-name $AppName | ConvertFrom-Json
    $AppId = $AppInfo.appId
    $ObjectId = $AppInfo.id
}

Write-Host "Using app with AppId: $AppId (ObjectId: $ObjectId)"

# Get or create service principal
$ExistingSp = az ad sp list --filter "clientAppId eq '$AppId'" --query '[0].id' -o tsv 2>$null

if ($ExistingSp -and $ExistingSp -ne "") {
    Write-Host "Found existing service principal: $ExistingSp"
    $SpObjectId = $ExistingSp
} else {
    Write-Host "Creating service principal..."
    $SpInfo = az ad sp create --id $AppId | ConvertFrom-Json
    $SpObjectId = $SpInfo.id
    Write-Host "Created service principal with ObjectId: $SpObjectId"
}

# Assign Owner role (adjust as needed for your security requirements)
Write-Host "Assigning Owner role to service principal..."
$scopePath = "/subscriptions/$SubscriptionId"

# Check if assignment already exists
$existingAssignment = az role assignment list --assignee $SpObjectId --role "Owner" --scope $scopePath --query '[0].id' -o tsv 2>$null

if (-not $existingAssignment -or $existingAssignment -eq "") {
    az role assignment create `
        --assignee $SpObjectId `
        --role "Owner" `
        --scope $scopePath
    Write-Host "Assigned Owner role"
} else {
    Write-Host "Owner role already assigned"
}

# Add federated credential for main branch
Write-Host "Adding federated credential for main branch..."
$tempFile1 = "$env:TEMP\github-main-$(Get-Random).json"
@{
    name     = "github-main"
    issuer   = "https://token.actions.githubusercontent.com"
    subject  = "repo:${GitHubRepo}:ref:refs/heads/main"
    audiences = @("api://AzureADTokenExchange")
} | ConvertTo-Json | Out-File -FilePath $tempFile1 -Encoding utf8 -Force

az ad app federated-credential create `
    --id $ObjectId `
    --parameters "@$tempFile1" 2>$null || Write-Host "Federated credential for main may already exist"

Remove-Item $tempFile1 -Force -ErrorAction SilentlyContinue

# Add federated credential for all branches
Write-Host "Adding federated credential for all branches..."
$tempFile2 = "$env:TEMP\github-all-$(Get-Random).json"
@{
    name     = "github-all"
    issuer   = "https://token.actions.githubusercontent.com"
    subject  = "repo:${GitHubRepo}:*"
    audiences = @("api://AzureADTokenExchange")
} | ConvertTo-Json | Out-File -FilePath $tempFile2 -Encoding utf8 -Force

az ad app federated-credential create `
    --id $ObjectId `
    --parameters "@$tempFile2" 2>$null || Write-Host "Federated credential for all branches may already exist"

Remove-Item $tempFile2 -Force -ErrorAction SilentlyContinue

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
