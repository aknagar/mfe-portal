@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param userPrincipalId string = ''

param tags object = { }

param infra_acr_outputs_name string

param logs_infra_outputs_name string

param daprRedis_outputs_hostname string

// Application Insights connection string — used to wire the ACA-managed OpenTelemetry collector.
// This parameter is threaded from main.bicep via appinsights_infra.outputs.appInsightsConnectionString.
// NOTE: This parameter and the appInsightsConfiguration / openTelemetryConfiguration properties below
// were added manually because Azure.Provisioning.AppContainers v1.1.0 does not expose these as typed
// properties. Re-apply this block if `aspire publish` regenerates this file.
param appinsights_infra_outputs_appInsightsConnectionString string

resource infra_mi 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' = {
  name: take('infra_mi-${uniqueString(resourceGroup().id)}', 128)
  location: location
  tags: tags
}

resource infra_acr 'Microsoft.ContainerRegistry/registries@2025-04-01' existing = {
  name: infra_acr_outputs_name
}

resource infra_acr_infra_mi_AcrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(infra_acr.id, infra_mi.id, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d'))
  properties: {
    principalId: infra_mi.properties.principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
    principalType: 'ServicePrincipal'
  }
  scope: infra_acr
}

resource logs_infra 'Microsoft.OperationalInsights/workspaces@2025-02-01' existing = {
  name: logs_infra_outputs_name
}

resource infra 'Microsoft.App/managedEnvironments@2024-10-02-preview' = {
  name: take('infra${uniqueString(resourceGroup().id)}', 24)
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logs_infra.properties.customerId
        sharedKey: logs_infra.listKeys().primarySharedKey
      }
    }
    // Wire Application Insights as a named OTel destination at the ACA environment level.
    // This enables the ACA-managed OTel collector sidecar to forward traces and logs to App Insights
    // without requiring the Azure Monitor SDK inside each container app.
    appInsightsConfiguration: {
      connectionString: appinsights_infra_outputs_appInsightsConnectionString
    }
    // Route OTLP traces and logs emitted by container apps to Application Insights.
    // includeDapr: false excludes Dapr sidecar internal traces (inter-service communication noise).
    // Note: appInsights does not support metricsConfiguration — omit that block.
    openTelemetryConfiguration: {
      tracesConfiguration: {
        destinations: [
          'appInsights'
        ]
        includeDapr: false
      }
      logsConfiguration: {
        destinations: [
          'appInsights'
        ]
      }
    }
    workloadProfiles: [
      {
        name: 'consumption'
        workloadProfileType: 'Consumption'
      }
    ]
  }
  tags: tags
}

resource aspireDashboard 'Microsoft.App/managedEnvironments/dotNetComponents@2024-10-02-preview' = {
  name: 'aspire-dashboard'
  properties: {
    componentType: 'AspireDashboard'
  }
  parent: infra
}

resource daprPubSub 'Microsoft.App/managedEnvironments/daprComponents@2025-01-01' = {
  name: 'pubsub'
  properties: {
    componentType: 'pubsub.redis'
    metadata: [
      {
        name: 'redisHost'
        value: concat(daprRedis_outputs_hostname, ':10000')
      }
      {
        name: 'enableTLS'
        value: 'true'
      }
      {
        name: 'useEntraID'
        value: 'true'
      }
    ]
    scopes: [
      'augmentservice'
    ]
    version: 'v1'
  }
  parent: infra
}

resource daprStateStore 'Microsoft.App/managedEnvironments/daprComponents@2025-01-01' = {
  name: 'statestore'
  properties: {
    componentType: 'state.redis'
    metadata: [
      {
        name: 'redisHost'
        value: concat(daprRedis_outputs_hostname, ':10000')
      }
      {
        name: 'enableTLS'
        value: 'true'
      }
      {
        name: 'useEntraID'
        value: 'true'
      }
      {
        name: 'actorStateStore'
        value: 'true'
      }
    ]
    scopes: [
      'augmentservice'
    ]
    version: 'v1'
  }
  parent: infra
}

output AZURE_LOG_ANALYTICS_WORKSPACE_NAME string = logs_infra.name

output AZURE_LOG_ANALYTICS_WORKSPACE_ID string = logs_infra.id

output AZURE_CONTAINER_REGISTRY_NAME string = infra_acr.name

output AZURE_CONTAINER_REGISTRY_ENDPOINT string = infra_acr.properties.loginServer

output AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID string = infra_mi.id

output AZURE_CONTAINER_APPS_ENVIRONMENT_NAME string = infra.name

output AZURE_CONTAINER_APPS_ENVIRONMENT_ID string = infra.id

output AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN string = infra.properties.defaultDomain