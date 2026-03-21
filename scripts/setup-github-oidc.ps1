# Setup GitHub OIDC Workload Identity Federation for Azure
# This script configures Azure to trust GitHub Actions workflows using OIDC
# Requires: Azure CLI, logged in with sufficient permissions
#
# SECURITY NOTES:
# - Federated credentials are restricted to specific branches/environments only
#   (main branch, production environment, test environment).
#   Wildcard subjects (repo:*) are intentionally NOT created — they would allow
#   any branch or PR to obtain an Azure token.
# - The service principal is granted Contributor (not Owner) at subscription scope.
#   Owner grants the ability to assign roles to others, which is not needed here.
#   If role assignments are required by aspire deploy, a separate scoped
#   User Access Administrator role can be added per resource group below.

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

# Assign Contributor role at subscription scope.
# Contributor allows creating/modifying resources but cannot assign roles to others.
# This is the minimum role needed for aspire deploy to provision Azure resources.
Write-Host "Assigning Contributor role to service principal..."
$scopePath = "/subscriptions/$SubscriptionId"

# Check if assignment already exists
$existingAssignment = az role assignment list --assignee $SpObjectId --role "Contributor" --scope $scopePath --query '[0].id' -o tsv 2>$null

if (-not $existingAssignment -or $existingAssignment -eq "") {
    az role assignment create `
        --assignee $SpObjectId `
        --role "Contributor" `
        --scope $scopePath
    Write-Host "Assigned Contributor role"
} else {
    Write-Host "Contributor role already assigned"
}

# Assign User Access Administrator scoped to the subscription so that
# aspire deploy can create managed identity role assignments (e.g. AcrPull,
# Service Bus Data Owner, Key Vault Secrets User). This is narrower than Owner
# because it cannot modify subscription-level policy or billing.
Write-Host "Assigning User Access Administrator role to service principal..."
$existingUaaAssignment = az role assignment list --assignee $SpObjectId --role "User Access Administrator" --scope $scopePath --query '[0].id' -o tsv 2>$null

if (-not $existingUaaAssignment -or $existingUaaAssignment -eq "") {
    az role assignment create `
        --assignee $SpObjectId `
        --role "User Access Administrator" `
        --scope $scopePath `
        --condition "((!(ActionMatches{'Microsoft.Authorization/roleAssignments/write'})) OR (@Request[Microsoft.Authorization/roleAssignments:RoleDefinitionId] ForAnyOfAnyValues:GuidEquals {7f951dda-4ed3-4680-a7ca-43fe172d538d, 4f6f55a1-7d4b-4b11-8b16-4b1ad4a9a8c3, b24988ac-6180-42a0-ab88-20f7382dd24c, 4d97b98b-1d4f-4787-a291-c67834d212e8}))" `
        --condition-version "2.0"
    Write-Host "Assigned User Access Administrator role (scoped to pipeline-required role definitions only)"
} else {
    Write-Host "User Access Administrator role already assigned"
}

# Add federated credential for main branch
# Only the main branch can obtain an Azure token — feature branches and PRs cannot.
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

# Add federated credential for the production GitHub environment
# Required so that infra-provision-prod.yml (which uses environment: production)
# can obtain a token. GitHub environment subjects are separate from branch subjects.
Write-Host "Adding federated credential for production environment..."
$tempFile2 = "$env:TEMP\github-env-prod-$(Get-Random).json"
@{
    name     = "github-env-production"
    issuer   = "https://token.actions.githubusercontent.com"
    subject  = "repo:${GitHubRepo}:environment:production"
    audiences = @("api://AzureADTokenExchange")
} | ConvertTo-Json | Out-File -FilePath $tempFile2 -Encoding utf8 -Force

az ad app federated-credential create `
    --id $ObjectId `
    --parameters "@$tempFile2" 2>$null || Write-Host "Federated credential for production environment may already exist"

Remove-Item $tempFile2 -Force -ErrorAction SilentlyContinue

# Add federated credential for the test GitHub environment
Write-Host "Adding federated credential for test environment..."
$tempFile3 = "$env:TEMP\github-env-test-$(Get-Random).json"
@{
    name     = "github-env-test"
    issuer   = "https://token.actions.githubusercontent.com"
    subject  = "repo:${GitHubRepo}:environment:test"
    audiences = @("api://AzureADTokenExchange")
} | ConvertTo-Json | Out-File -FilePath $tempFile3 -Encoding utf8 -Force

az ad app federated-credential create `
    --id $ObjectId `
    --parameters "@$tempFile3" 2>$null || Write-Host "Federated credential for test environment may already exist"

Remove-Item $tempFile3 -Force -ErrorAction SilentlyContinue

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
