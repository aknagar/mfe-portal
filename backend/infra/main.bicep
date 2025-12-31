@minLength(1)
@maxLength(64)
@description('Name of the environment that will be created and used to generate a short unique hash used in all resources.')
param environmentName string

@minLength(1)
@description('Primary location for all resources.')
param location string = resourceGroup().location

@description('Container image repository URL')
param containerRegistryUrl string

@description('Container image name and tag')
param containerImageName string = 'augmentservice:latest'

@description('Azure Cache for Redis SKU')
param redisSku string = 'Basic'

@description('Azure Cache for Redis capacity')
param redisCapacity int = 0

@description('Container port')
param containerPort int = 8080

@description('CPU cores for container app')
param containerCpus string = '0.5'

@description('Memory for container app')
param containerMemory string = '1Gi'

@description('Number of replicas')
param containerReplicas int = 1

@description('Dapr HTTP port')
param daprHttpPort int = 3500

@description('Dapr gRPC port')
param daprGrpcPort int = 50001

var resourceToken = uniqueString(resourceGroup().id)
var tags = { 'azd-env-name': environmentName }

// User-Managed Identity for all services
resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'id-${resourceToken}'
  location: location
  tags: tags
}

// Log Analytics workspace
resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: 'log-${resourceToken}'
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

// Container Registry
resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-01-01-preview' = {
  name: 'acr${resourceToken}'
  location: location
  tags: tags
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: true
    publicNetworkAccess: 'Enabled'
    networkRuleBypassOptions: 'AzureServices'
    policies: {
      quarantinePolicy: {
        status: 'disabled'
      }
      trustPolicy: {
        type: 'Notary'
        status: 'disabled'
      }
    }
  }
}

// Container Apps Environment
resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2023-04-01-preview' = {
  name: 'cae-${resourceToken}'
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalyticsWorkspace.properties.customerId
        sharedKey: logAnalyticsWorkspace.listKeys().primarySharedKey
      }
    }
    zoneRedundant: false
  }
}

// Azure Cache for Redis
resource redis 'Microsoft.Cache/redis@2023-04-01' = {
  name: 'redis-${resourceToken}'
  location: location
  tags: tags
  properties: {
    sku: {
      name: redisSku
      family: 'C'
      capacity: redisCapacity
    }
    enableNonSslPort: false
    minimumTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    redisConfiguration: {
      'maxmemory-policy': 'allkeys-lru'
    }
  }
}

// Azure Key Vault for secrets management
resource keyVault 'Microsoft.KeyVault/vaults@2023-02-01' = {
  name: 'kv-${resourceToken}'
  location: location
  tags: tags
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true
    publicNetworkAccess: 'Enabled'
  }
}

// Store Redis connection string in Key Vault
resource redisConnectionStringSecret 'Microsoft.KeyVault/vaults/secrets@2023-02-01' = {
  parent: keyVault
  name: 'RedisConnectionString'
  properties: {
    value: '${redis.properties.hostName}:${redis.properties.port}?ssl=True&password=${redis.listKeys().primaryKey}'
  }
}

// Grant Managed Identity permission to read secrets from Key Vault
// Role ID: 4633458b-17de-408a-b874-0445c86b69e6 = Key Vault Secrets User
resource keyVaultSecretsAccessRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: keyVault
  name: guid(keyVault.id, managedIdentity.id, '4633458b-17de-408a-b874-0445c86b69e6')
  properties: {
    principalId: managedIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
  }
}

// Outputs
output containerAppsEnvironmentId string = containerAppsEnvironment.id
output containerAppsEnvironmentName string = containerAppsEnvironment.name
output containerRegistryUrl string = containerRegistry.properties.loginServer
output containerRegistryName string = containerRegistry.name
output redisPrimaryConnectionString string = '${redis.properties.hostName}:${redis.properties.port}?ssl=True'
output logAnalyticsWorkspaceId string = logAnalyticsWorkspace.id
output logAnalyticsWorkspaceName string = logAnalyticsWorkspace.name
output keyVaultId string = keyVault.id
output keyVaultName string = keyVault.name
output keyVaultUri string = keyVault.properties.vaultUri
output redisConnectionStringSecretId string = redisConnectionStringSecret.id
output managedIdentityId string = managedIdentity.id
output managedIdentityPrincipalId string = managedIdentity.properties.principalId
output managedIdentityName string = managedIdentity.name
output resourceToken string = resourceToken
