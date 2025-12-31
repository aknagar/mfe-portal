@description('Name of the Container Apps Environment')
param containerAppsEnvironmentName string

@description('Name of the resource group')
param resourceGroupName string

@description('Location for resources')
param location string

@description('Container image URL')
param containerImage string

@description('Container port')
param containerPort int = 8080

@description('Container CPU cores')
param containerCpus string = '0.5'

@description('Container memory')
param containerMemory string = '1Gi'

@description('Number of replicas')
param containerReplicas int = 1

@description('Redis connection string')
param redisConnectionString string

@description('Dapr HTTP endpoint')
param daprHttpEndpoint string

@description('Dapr gRPC endpoint')
param daprGrpcEndpoint string

@description('Resource token for naming')
param resourceToken string

@description('Environment name')
param environmentName string

@description('Key Vault name for secret retrieval')
param keyVaultName string

@description('Key Vault resource ID')
param keyVaultResourceId string

@description('Key Vault URI')
param keyVaultUri string

@description('User-Assigned Managed Identity Resource ID')
param managedIdentityId string

@description('User-Assigned Managed Identity Principal ID')
param managedIdentityPrincipalId string

var tags = { 'azd-env-name': environmentName }

// Get reference to Container Apps Environment
resource containerAppsEnv 'Microsoft.App/managedEnvironments@2023-04-01-preview' existing = {
  name: containerAppsEnvironmentName
  scope: resourceGroup(resourceGroupName)
}

// Get reference to Key Vault
resource keyVault 'Microsoft.KeyVault/vaults@2023-02-01' existing = {
  name: keyVaultName
  scope: resourceGroup(resourceGroupName)
}

// AugmentService Container App with Dapr
resource augmentServiceApp 'Microsoft.App/containerApps@2023-04-01-preview' = {
  name: 'augmentservice-${resourceToken}'
  location: location
  tags: tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentityId}': {}
    }
  }
  properties: {
    environmentId: containerAppsEnv.id
    configuration: {
      ingress: {
        external: true
        targetPort: containerPort
        transport: 'http'
        allowInsecure: false
        traffic: [
          {
            latestRevision: true
            weight: 100
          }
        ]
      }
      dapr: {
        enabled: true
        appId: 'augmentservice'
        appPort: containerPort
        appProtocol: 'http'
      }
      secrets: [
        {
          name: 'redis-connection-string'
          keyVaultUrl: '${keyVaultUri}secrets/RedisConnectionString'
          identity: 'System'
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'augmentservice'
          image: containerImage
          resources: {
            cpu: json(containerCpus)
            memory: containerMemory
          }
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Production'
            }
            {
              name: 'ASPNETCORE_URLS'
              value: 'http://+:${containerPort}'
            }
            {
              name: 'DAPR_HTTP_ENDPOINT'
              value: daprHttpEndpoint
            }
            {
              name: 'DAPR_GRPC_ENDPOINT'
              value: daprGrpcEndpoint
            }
            {
              name: 'REDIS_CONNECTION_STRING'
              secretRef: 'redis-connection-string'
            }
          ]
        }
      ]
      scale: {
        minReplicas: max(1, containerReplicas)
        maxReplicas: max(containerReplicas, 10)
        rules: [
          {
            name: 'http-requests'
            http: {
              metadata: {
                concurrentRequests: '10'
              }
            }
          }
        ]
      }
    }
  }
}

// Grant Managed Identity permission to read secrets from Key Vault
// Role ID: 4633458b-17de-408a-b874-0445c86b69e6 = Key Vault Secrets User
resource keyVaultSecretsAccessRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: keyVault
  name: guid(keyVault.id, managedIdentityPrincipalId, '4633458b-17de-408a-b874-0445c86b69e6')
  properties: {
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
  }
}

// Outputs
output fqdn string = augmentServiceApp.properties.configuration.ingress.fqdn
output containerAppId string = augmentServiceApp.id
output containerAppName string = augmentServiceApp.name
output managedIdentityId string = managedIdentityId
output managedIdentityPrincipalId string = managedIdentityPrincipalId
