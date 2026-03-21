@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param infra_outputs_azure_container_apps_environment_default_domain string

param infra_outputs_azure_container_apps_environment_id string

@secure()
param redis_password_value string

// WARNING: This container has no persistent volume mount. Data will be lost on container restart.
// For production use, provision an Azure Database for PostgreSQL Flexible Server instead.
resource redis 'Microsoft.App/containerApps@2025-01-01' = {
  name: 'redis'
  location: location
  properties: {
    configuration: {
      secrets: [
        {
          name: 'redis-password'
          value: redis_password_value
        }
      ]
      activeRevisionsMode: 'Single'
      ingress: {
        external: false
        targetPort: 6379
        transport: 'tcp'
      }
    }
    environmentId: infra_outputs_azure_container_apps_environment_id
    template: {
      containers: [
        {
          image: 'docker.io/library/redis:8.2'
          name: 'redis'
          command: [
            '/bin/sh'
          ]
          args: [
            '-c'
            'redis-server --requirepass \$REDIS_PASSWORD'
          ]
          env: [
            {
              name: 'REDIS_PASSWORD'
              secretRef: 'redis-password'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
    }
  }
}