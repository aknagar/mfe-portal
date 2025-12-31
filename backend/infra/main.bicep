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

// Outputs
output containerAppsEnvironmentId string = containerAppsEnvironment.id
output containerAppsEnvironmentName string = containerAppsEnvironment.name
output containerRegistryUrl string = containerRegistry.properties.loginServer
output containerRegistryName string = containerRegistry.name
output redisPrimaryConnectionString string = '${redis.properties.hostName}:${redis.properties.port}?ssl=True'
output logAnalyticsWorkspaceId string = logAnalyticsWorkspace.id
output logAnalyticsWorkspaceName string = logAnalyticsWorkspace.name
output resourceToken string = resourceToken
