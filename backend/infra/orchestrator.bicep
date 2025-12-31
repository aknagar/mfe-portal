@description('Azure Region location')
param location string = resourceGroup().location

@description('Environment name')
param environmentName string

@description('Container Registry URL')
param containerRegistryUrl string

@description('Container image name and tag')
param containerImageName string = 'augmentservice:latest'

@description('Redis SKU')
param redisSku string = 'Basic'

@description('Redis capacity')
param redisCapacity int = 0

@description('Container port')
param containerPort int = 8080

@description('Container CPU cores')
param containerCpus string = '0.5'

@description('Container memory')
param containerMemory string = '1Gi'

@description('Number of container replicas')
param containerReplicas int = 1

@description('Dapr HTTP port')
param daprHttpPort int = 3500

@description('Dapr gRPC port')
param daprGrpcPort int = 50001

// Deploy infrastructure resources
module infrastructure 'main.bicep' = {
  name: 'infrastructure'
  params: {
    environmentName: environmentName
    location: location
    containerRegistryUrl: containerRegistryUrl
    containerImageName: containerImageName
    redisSku: redisSku
    redisCapacity: redisCapacity
    containerPort: containerPort
    containerCpus: containerCpus
    containerMemory: containerMemory
    containerReplicas: containerReplicas
    daprHttpPort: daprHttpPort
    daprGrpcPort: daprGrpcPort
  }
}

// Deploy AugmentService container app with Dapr
module augmentService 'container-app.bicep' = {
  name: 'augmentservice-app'
  params: {
    containerAppsEnvironmentName: infrastructure.outputs.containerAppsEnvironmentName
    resourceGroupName: resourceGroup().name
    location: location
    containerImage: '${infrastructure.outputs.containerRegistryUrl}/${containerImageName}'
    containerPort: containerPort
    containerCpus: containerCpus
    containerMemory: containerMemory
    containerReplicas: containerReplicas
    redisConnectionString: infrastructure.outputs.redisPrimaryConnectionString
    daprHttpEndpoint: 'http://localhost:${daprHttpPort}'
    daprGrpcEndpoint: 'http://localhost:${daprGrpcPort}'
    resourceToken: infrastructure.outputs.resourceToken
    environmentName: environmentName
    keyVaultName: infrastructure.outputs.keyVaultName
    keyVaultResourceId: infrastructure.outputs.keyVaultId
    keyVaultUri: infrastructure.outputs.keyVaultUri
    managedIdentityId: infrastructure.outputs.managedIdentityId
    managedIdentityPrincipalId: infrastructure.outputs.managedIdentityPrincipalId
    applicationInsightsConnectionString: infrastructure.outputs.applicationInsightsConnectionString
    applicationInsightsInstrumentationKey: infrastructure.outputs.applicationInsightsInstrumentationKey
  }
}

// Outputs
output containerAppsEnvironmentName string = infrastructure.outputs.containerAppsEnvironmentName
output containerRegistryUrl string = infrastructure.outputs.containerRegistryUrl
output containerRegistryName string = infrastructure.outputs.containerRegistryName
output augmentServiceFqdn string = augmentService.outputs.fqdn
output augmentServiceUrl string = 'https://${augmentService.outputs.fqdn}'
output redisPrimaryConnectionString string = infrastructure.outputs.redisPrimaryConnectionString
output logAnalyticsWorkspaceId string = infrastructure.outputs.logAnalyticsWorkspaceId
output applicationInsightsConnectionString string = infrastructure.outputs.applicationInsightsConnectionString
output applicationInsightsInstrumentationKey string = infrastructure.outputs.applicationInsightsInstrumentationKey
