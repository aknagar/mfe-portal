targetScope = 'subscription'

param resourceGroupName string

param location string

param principalId string

resource rg 'Microsoft.Resources/resourceGroups@2023-07-01' = {
  name: resourceGroupName
  location: location
}

module infra_acr 'infra-acr/infra-acr.bicep' = {
  name: 'infra-acr'
  scope: rg
  params: {
    location: location
  }
}

module infra 'infra/infra.bicep' = {
  name: 'infra'
  scope: rg
  params: {
    location: location
    infra_acr_outputs_name: infra_acr.outputs.name
    logs_infra_outputs_name: logs_infra.outputs.name
    userPrincipalId: principalId
  }
}

module messaging 'messaging/messaging.bicep' = {
  name: 'messaging'
  scope: rg
  params: {
    location: location
  }
}

module logs_infra 'logs-infra/logs-infra.bicep' = {
  name: 'logs-infra'
  scope: rg
  params: {
    location: location
  }
}

module appinsights_infra 'appinsights-infra/appinsights-infra.bicep' = {
  name: 'appinsights-infra'
  scope: rg
  params: {
    location: location
    logs_infra_outputs_loganalyticsworkspaceid: logs_infra.outputs.logAnalyticsWorkspaceId
  }
}

module keyvault 'keyvault/keyvault.bicep' = {
  name: 'keyvault'
  scope: rg
  params: {
    location: location
  }
}

module augmentservice_identity 'augmentservice-identity/augmentservice-identity.bicep' = {
  name: 'augmentservice-identity'
  scope: rg
  params: {
    location: location
  }
}

module augmentservice_roles_messaging 'augmentservice-roles-messaging/augmentservice-roles-messaging.bicep' = {
  name: 'augmentservice-roles-messaging'
  scope: rg
  params: {
    location: location
    messaging_outputs_name: messaging.outputs.name
    principalId: augmentservice_identity.outputs.principalId
  }
}

module augmentservice_roles_keyvault 'augmentservice-roles-keyvault/augmentservice-roles-keyvault.bicep' = {
  name: 'augmentservice-roles-keyvault'
  scope: rg
  params: {
    location: location
    keyvault_outputs_name: keyvault.outputs.name
    principalId: augmentservice_identity.outputs.principalId
  }
}

output infra_acr_name string = infra_acr.outputs.name

output infra_acr_loginServer string = infra_acr.outputs.loginServer

output infra_AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID string = infra.outputs.AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID

output infra_AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN string = infra.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN

output infra_AZURE_CONTAINER_APPS_ENVIRONMENT_ID string = infra.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_ID

output infra_AZURE_CONTAINER_REGISTRY_ENDPOINT string = infra.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT

output augmentservice_identity_id string = augmentservice_identity.outputs.id

output messaging_serviceBusEndpoint string = messaging.outputs.serviceBusEndpoint

output messaging_serviceBusHostName string = messaging.outputs.serviceBusHostName

output appinsights_infra_appInsightsConnectionString string = appinsights_infra.outputs.appInsightsConnectionString

output keyvault_vaultUri string = keyvault.outputs.vaultUri

output augmentservice_identity_clientId string = augmentservice_identity.outputs.clientId