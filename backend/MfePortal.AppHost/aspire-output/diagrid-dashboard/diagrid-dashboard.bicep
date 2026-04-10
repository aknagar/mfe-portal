@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param infra_outputs_azure_container_apps_environment_default_domain string

param infra_outputs_azure_container_apps_environment_id string

resource diagrid_dashboard 'Microsoft.App/containerApps@2025-01-01' = {
  name: 'diagrid-dashboard'
  location: location
  properties: {
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 8080
        transport: 'http'
      }
    }
    environmentId: infra_outputs_azure_container_apps_environment_id
    template: {
      containers: [
        {
          image: 'ghcr.io/diagridio/diagrid-dashboard:0.0.1'
          name: 'diagrid-dashboard'
        }
      ]
      scale: {
        minReplicas: 1
      }
    }
  }
}