@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource infra_logs 'Microsoft.OperationalInsights/workspaces@2025-02-01' = {
  name: take('infralogs-${uniqueString(resourceGroup().id)}', 63)
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
  }
  tags: {
    'aspire-resource-name': 'infra-logs'
  }
}

output logAnalyticsWorkspaceId string = infra_logs.id

output name string = infra_logs.name