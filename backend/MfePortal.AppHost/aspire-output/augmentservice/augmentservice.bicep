@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param infra_outputs_azure_container_apps_environment_default_domain string

param infra_outputs_azure_container_apps_environment_id string

param augmentservice_containerimage string

param augmentservice_identity_outputs_id string

param augmentservice_containerport string

param postgres_outputs_connectionstring string

param postgres_outputs_hostname string

param messaging_outputs_servicebusendpoint string

param messaging_outputs_servicebushostname string

param azureadtenantid_value string

param azureadclientid_value string

param appinsights_infra_outputs_appinsightsconnectionstring string

param keyvault_outputs_vaulturi string

param augmentservice_identity_outputs_clientid string

param augmentservice_identity_outputs_principalname string

param infra_outputs_azure_container_registry_endpoint string

param infra_outputs_azure_container_registry_managed_identity_id string

resource augmentservice 'Microsoft.App/containerApps@2025-02-02-preview' = {
  name: 'augmentservice'
  location: location
  properties: {
    configuration: {
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
      dapr: {
        enabled: true
        appId: 'augmentservice'
        appProtocol: 'http'
        appPort: 8080
      }
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
              value: '${postgres_outputs_connectionstring};Database=productdb'
            }
            {
              name: 'PRODUCTDB_HOST'
              value: postgres_outputs_hostname
            }
            {
              name: 'PRODUCTDB_PORT'
              value: '5432'
            }
            {
              name: 'PRODUCTDB_URI'
              value: 'postgresql://${postgres_outputs_hostname}/productdb'
            }
            {
              name: 'PRODUCTDB_JDBCCONNECTIONSTRING'
              value: 'jdbc:postgresql://${postgres_outputs_hostname}/productdb?sslmode=require&authenticationPluginClassName=com.azure.identity.extensions.jdbc.postgresql.AzurePostgresqlAuthenticationPlugin'
            }
            {
              name: 'PRODUCTDB_DATABASENAME'
              value: 'productdb'
            }
            {
              name: 'ConnectionStrings__weatherdb'
              value: '${postgres_outputs_connectionstring};Database=weatherdb'
            }
            {
              name: 'WEATHERDB_HOST'
              value: postgres_outputs_hostname
            }
            {
              name: 'WEATHERDB_PORT'
              value: '5432'
            }
            {
              name: 'WEATHERDB_URI'
              value: 'postgresql://${postgres_outputs_hostname}/weatherdb'
            }
            {
              name: 'WEATHERDB_JDBCCONNECTIONSTRING'
              value: 'jdbc:postgresql://${postgres_outputs_hostname}/weatherdb?sslmode=require&authenticationPluginClassName=com.azure.identity.extensions.jdbc.postgresql.AzurePostgresqlAuthenticationPlugin'
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
              name: 'AzureAd__TenantId'
              value: azureadtenantid_value
            }
            {
              name: 'AzureAd__ClientId'
              value: azureadclientid_value
            }
            {
              name: 'AzureAd__Audience'
              value: 'api://${azureadclientid_value}'
            }
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              value: appinsights_infra_outputs_appinsightsconnectionstring
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
        rules: [
          {
            name: 'postgres-scaler'
            custom: {
              type: 'postgresql'
              metadata: {
                host: postgres_outputs_hostname
                userName: augmentservice_identity_outputs_principalname
                dbName: 'productdb'
                sslmode: 'require'
                targetQueryValue: '5'
                query: 'SELECT COUNT(*) FROM pg_stat_activity WHERE state = \'active\''
              }
              identity: augmentservice_identity_outputs_id
            }
          }
        ]
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
