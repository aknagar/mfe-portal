@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param infra_outputs_azure_container_apps_environment_default_domain string

param infra_outputs_azure_container_apps_environment_id string

param augmentservice_containerimage string

param augmentservice_identity_outputs_id string

param augmentservice_containerport string

@secure()
param postgres_password_value string

param messaging_outputs_servicebusendpoint string

param messaging_outputs_servicebushostname string

@secure()
param redis_password_value string

@secure()
param appinsights_infra_outputs_appinsightsconnectionstring string

param keyvault_outputs_vaulturi string

param augmentservice_identity_outputs_clientid string

param infra_outputs_azure_container_registry_endpoint string

param infra_outputs_azure_container_registry_managed_identity_id string

resource augmentservice 'Microsoft.App/containerApps@2025-02-02-preview' = {
  name: 'augmentservice'
  location: location
  properties: {
    configuration: {
      secrets: [
        {
          name: 'connectionstrings--productdb'
          value: 'Host=postgres;Port=5432;Username=postgres;Password=${postgres_password_value};Database=productdb'
        }
        {
          name: 'productdb-password'
          value: postgres_password_value
        }
        {
          name: 'productdb-uri'
          value: 'postgresql://postgres:${uriComponent(postgres_password_value)}@postgres:5432/productdb'
        }
        {
          name: 'connectionstrings--weatherdb'
          value: 'Host=postgres;Port=5432;Username=postgres;Password=${postgres_password_value};Database=weatherdb'
        }
        {
          name: 'weatherdb-password'
          value: postgres_password_value
        }
        {
          name: 'weatherdb-uri'
          value: 'postgresql://postgres:${uriComponent(postgres_password_value)}@postgres:5432/weatherdb'
        }
        {
          name: 'connectionstrings--redis'
          value: 'redis:6379,password=${redis_password_value}'
        }
        {
          name: 'redis-password'
          value: redis_password_value
        }
        {
          name: 'redis-uri'
          value: 'redis://:${uriComponent(redis_password_value)}@redis:6379'
        }
        {
          name: 'appinsights-connection-string'
          value: appinsights_infra_outputs_appinsightsconnectionstring
        }
      ]
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: int(augmentservice_containerport)
        transport: 'http'
      }
      registries: [
        {
          server: infra_outputs_azure_container_registry_endpoint
          identity: infra_outputs_azure_container_registry_managed_identity_id
        }
      ]
      runtime: {
        dotnet: {
          autoConfigureDataProtection: true
        }
      }
    }
    environmentId: infra_outputs_azure_container_apps_environment_id
    template: {
      containers: [
        {
          image: augmentservice_containerimage
          name: 'augmentservice'
          env: [
            {
              name: 'OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EXCEPTION_LOG_ATTRIBUTES'
              value: 'true'
            }
            {
              name: 'OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EVENT_LOG_ATTRIBUTES'
              value: 'true'
            }
            {
              name: 'OTEL_DOTNET_EXPERIMENTAL_OTLP_RETRY'
              value: 'in_memory'
            }
            {
              name: 'ASPNETCORE_FORWARDEDHEADERS_ENABLED'
              value: 'true'
            }
            {
              name: 'HTTP_PORTS'
              value: augmentservice_containerport
            }
            {
              name: 'ConnectionStrings__productdb'
              secretRef: 'connectionstrings--productdb'
            }
            {
              name: 'PRODUCTDB_HOST'
              value: 'postgres'
            }
            {
              name: 'PRODUCTDB_PORT'
              value: '5432'
            }
            {
              name: 'PRODUCTDB_USERNAME'
              value: 'postgres'
            }
            {
              name: 'PRODUCTDB_PASSWORD'
              secretRef: 'productdb-password'
            }
            {
              name: 'PRODUCTDB_URI'
              secretRef: 'productdb-uri'
            }
            {
              name: 'PRODUCTDB_JDBCCONNECTIONSTRING'
              value: 'jdbc:postgresql://postgres:5432/productdb'
            }
            {
              name: 'PRODUCTDB_DATABASENAME'
              value: 'productdb'
            }
            {
              name: 'ConnectionStrings__weatherdb'
              secretRef: 'connectionstrings--weatherdb'
            }
            {
              name: 'WEATHERDB_HOST'
              value: 'postgres'
            }
            {
              name: 'WEATHERDB_PORT'
              value: '5432'
            }
            {
              name: 'WEATHERDB_USERNAME'
              value: 'postgres'
            }
            {
              name: 'WEATHERDB_PASSWORD'
              secretRef: 'weatherdb-password'
            }
            {
              name: 'WEATHERDB_URI'
              secretRef: 'weatherdb-uri'
            }
            {
              name: 'WEATHERDB_JDBCCONNECTIONSTRING'
              value: 'jdbc:postgresql://postgres:5432/weatherdb'
            }
            {
              name: 'WEATHERDB_DATABASENAME'
              value: 'weatherdb'
            }
            {
              name: 'ConnectionStrings__messaging'
              value: messaging_outputs_servicebusendpoint
            }
            {
              name: 'MESSAGING_HOST'
              value: messaging_outputs_servicebushostname
            }
            {
              name: 'MESSAGING_URI'
              value: messaging_outputs_servicebusendpoint
            }
            {
              name: 'ConnectionStrings__redis'
              secretRef: 'connectionstrings--redis'
            }
            {
              name: 'REDIS_HOST'
              value: 'redis'
            }
            {
              name: 'REDIS_PORT'
              value: '6379'
            }
            {
              name: 'REDIS_PASSWORD'
              secretRef: 'redis-password'
            }
            {
              name: 'REDIS_URI'
              secretRef: 'redis-uri'
            }
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              secretRef: 'appinsights-connection-string'
            }
            {
              name: 'ConnectionStrings__keyvault'
              value: keyvault_outputs_vaulturi
            }
            {
              name: 'KEYVAULT_URI'
              value: keyvault_outputs_vaulturi
            }
            {
              name: 'AZURE_CLIENT_ID'
              value: augmentservice_identity_outputs_clientid
            }
            {
              name: 'AZURE_TOKEN_CREDENTIALS'
              value: 'ManagedIdentityCredential'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
      }
    }
  }
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${augmentservice_identity_outputs_id}': { }
      '${infra_outputs_azure_container_registry_managed_identity_id}': { }
    }
  }
}