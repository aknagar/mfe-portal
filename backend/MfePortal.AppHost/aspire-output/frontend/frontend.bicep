@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param infra_outputs_azure_container_apps_environment_default_domain string

param infra_outputs_azure_container_apps_environment_id string

resource frontend 'Microsoft.App/containerApps@2025-01-01' = {
  name: 'frontend'
  location: location
  properties: {
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 1234
        transport: 'http'
      }
    }
    environmentId: infra_outputs_azure_container_apps_environment_id
    template: {
      containers: [
        {
          image: 'infraacrescmmynaae3lk.azurecr.io/frontend:latest'
          name: 'frontend'
        }
      ]
      scale: {
        minReplicas: 1
      }
    }
  }
}