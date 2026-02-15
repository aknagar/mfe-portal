@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource logs_infra 'Microsoft.OperationalInsights/workspaces@2025-02-01' = {
  name: take('logsinfra-${uniqueString(resourceGroup().id)}', 63)
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
  }
  tags: {
    'aspire-resource-name': 'logs-infra'
  }
}

output logAnalyticsWorkspaceId string = logs_infra.id

output name string = logs_infra.name