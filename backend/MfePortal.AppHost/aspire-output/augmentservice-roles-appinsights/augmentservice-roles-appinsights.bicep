@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param appinsights_infra_outputs_name string

param principalId string

resource appinsights_infra 'Microsoft.Insights/components@2020-02-02' existing = {
  name: appinsights_infra_outputs_name
}

resource appinsights_MonitoringMetricsPublisher 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(appinsights_infra.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '3913510d-42f4-4e42-8a64-420c390055eb'))
  properties: {
    principalId: principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '3913510d-42f4-4e42-8a64-420c390055eb')
    principalType: 'ServicePrincipal'
  }
  scope: appinsights_infra
}
