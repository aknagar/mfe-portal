@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param applicationType string = 'web'

param kind string = 'web'

param logs_infra_outputs_loganalyticsworkspaceid string

resource appinsights_infra 'Microsoft.Insights/components@2020-02-02' = {
  name: take('appinsights_infra-${uniqueString(resourceGroup().id)}', 260)
  kind: kind
  location: location
  properties: {
    Application_Type: applicationType
    WorkspaceResourceId: logs_infra_outputs_loganalyticsworkspaceid
  }
  tags: {
    'aspire-resource-name': 'appinsights-infra'
  }
}

output appInsightsConnectionString string = appinsights_infra.properties.ConnectionString

output name string = appinsights_infra.name