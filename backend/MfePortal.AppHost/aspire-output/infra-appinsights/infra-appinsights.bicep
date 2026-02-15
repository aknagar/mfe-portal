@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param applicationType string = 'web'

param kind string = 'web'

param infra_logs_outputs_loganalyticsworkspaceid string

resource infra_appinsights 'Microsoft.Insights/components@2020-02-02' = {
  name: take('infra_appinsights-${uniqueString(resourceGroup().id)}', 260)
  kind: kind
  location: location
  properties: {
    Application_Type: applicationType
    WorkspaceResourceId: infra_logs_outputs_loganalyticsworkspaceid
  }
  tags: {
    'aspire-resource-name': 'infra-appinsights'
  }
}

output appInsightsConnectionString string = infra_appinsights.properties.ConnectionString

output name string = infra_appinsights.name