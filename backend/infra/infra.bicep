@minLength(1)
@maxLength(64)
@description('Name of the environment that will be created and used to generate a short unique hash used in all resources.')
param environmentName string

@minLength(1)
@description('Primary location for all resources.')
param location string = resourceGroup().location

@description('Container image repository URL')
param containerRegistryUrl string = ''

@description('Container image name and tag')
param containerImageName string = 'augmentservice:latest'

@description('Azure Cache for Redis SKU')
param redisSku string = 'Basic'

@description('Azure Cache for Redis capacity')
param redisCapacity int = 0

@description('Container port')
param containerPort int = 8080

@description('CPU cores for container app')
param containerCpus string = '0.5'

@description('Memory for container app')
param containerMemory string = '1Gi'

@description('Number of replicas')
param containerReplicas int = 1

@description('Dapr HTTP port')
param daprHttpPort int = 3500

@description('Dapr gRPC port')
param daprGrpcPort int = 50001

var resourceToken = uniqueString(resourceGroup().id)
var tags = { 'azd-env-name': environmentName }

// User-Managed Identity for all services
resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'id-${resourceToken}'
  location: location
  tags: tags
}

// Log Analytics workspace
resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: 'log-${resourceToken}'
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

// Application Insights
resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: 'appi-${resourceToken}'
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    RetentionInDays: 30
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
    WorkspaceResourceId: logAnalyticsWorkspace.id
  }
}

// Application Insights Dashboard
resource applicationInsightsDashboard 'Microsoft.Portal/dashboards@2020-09-01-preview' = {
  name: 'dashboard-${resourceToken}'
  location: location
  tags: union(tags, { 'hidden-title': 'Application Insights Dashboard' })
  properties: {
    lenses: [
      {
        order: 0
        parts: [
          {
            position: {
              x: 0
              y: 0
              rowSpan: 4
              colSpan: 6
            }
            metadata: {
              inputs: [
                {
                  name: 'ComponentId'
                  value: '/subscriptions/${subscription().subscriptionId}/resourceGroups/${resourceGroup().name}/providers/microsoft.insights/components/${applicationInsights.name}'
                }
                {
                  name: 'Scope'
                  value: '/subscriptions/${subscription().subscriptionId}/resourceGroups/${resourceGroup().name}/providers/microsoft.insights/components/${applicationInsights.name}'
                }
                {
                  name: 'PartId'
                  value: '3aa7efc5-f0b5-4ac6-918a-166b10495829'
                }
                {
                  name: 'Version'
                  value: '1.0'
                }
                {
                  name: 'TimeRange'
                  value: 'PT1H'
                }
                {
                  name: 'DashboardId'
                  value: ''
                }
              ]
              type: 'Extension/AppInsightsExtension/PartType/AppMapPartsGaleryPart'
              settings: {}
              asset: {
                idTemplate: '/subscriptions/{sid}/resourceGroups/{rg}/providers/microsoft.insights/components/{cname}'
              }
            }
          }
        ]
      }
    ]
    metadata: {
      model: {
        timeRange: {
          value: 'PT1H'
          type: 'MsPortalFx.Composition.Configuration.ValueTypes.TimeRange'
        }
        filterLocale: {
          value: 'en-us'
        }
        filters: {
          value: {
            MsPortalFx_TimeRange: {
              model: {
                format: 'utc'
                globalize: true
                timezone: 'Etc/UTC'
                isUTC: true
                endTime: null
                timeSpanMs: 3600000
                startTime: null
                options: {
                  skipFetchingTodaysForecast: false
                }
              }
              displayCache: {
                name: 'UTC Time'
                value: 'Past hour'
              }
              filteredPartIds: [
                '3aa7efc5-f0b5-4ac6-918a-166b10495829/timeRangeFilter'
              ]
            }
          }
        }
      }
    }
  }
}

// Container Registry
resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-01-01-preview' = {
  name: 'acr${resourceToken}'
  location: location
  tags: tags
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: true
    publicNetworkAccess: 'Enabled'
    networkRuleBypassOptions: 'AzureServices'
    policies: {
      quarantinePolicy: {
        status: 'disabled'
      }
      trustPolicy: {
        type: 'Notary'
        status: 'disabled'
      }
    }
  }
}

// Container Apps Environment
resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2023-04-01-preview' = {
  name: 'cae-${resourceToken}'
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalyticsWorkspace.properties.customerId
        sharedKey: logAnalyticsWorkspace.listKeys().primarySharedKey
      }
    }
    zoneRedundant: false
  }
}

// Azure Cache for Redis
resource redis 'Microsoft.Cache/redis@2023-04-01' = {
  name: 'redis-${resourceToken}'
  location: location
  tags: tags
  properties: {
    sku: {
      name: redisSku
      family: 'C'
      capacity: redisCapacity
    }
    enableNonSslPort: false
    minimumTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    redisConfiguration: {
      'maxmemory-policy': 'allkeys-lru'
    }
  }
}

// Azure Database for PostgreSQL - Flexible Server with Azure AD authentication
resource postgresqlServer 'Microsoft.DBforPostgreSQL/flexibleServers@2023-12-01-preview' = {
  name: 'pg-${resourceToken}'
  location: location
  tags: tags
  sku: {
    name: 'Standard_B1ms'
    tier: 'Burstable'
  }
  properties: {
    version: '15'
    administratorLogin: 'pgadmin'
    authConfig: {
      activeDirectoryAuth: 'Enabled'
      passwordAuth: 'Disabled'  // Disable password authentication
      tenantId: subscription().tenantId
    }
    storage: {
      storageSizeGB: 32
    }
    backup: {
      backupRetentionDays: 7
      geoRedundantBackup: 'Disabled'
    }
    network: {
      delegatedSubnetResourceId: ''
      privateDnsZoneArmResourceId: ''
    }
    highAvailability: {
      mode: 'Disabled'
    }
  }
}

// Set managed identity as PostgreSQL Azure AD administrator
resource postgresqlAADAdmin 'Microsoft.DBforPostgreSQL/flexibleServers/administrators@2023-12-01-preview' = {
  parent: postgresqlServer
  name: managedIdentity.properties.principalId
  properties: {
    principalName: managedIdentity.name
    principalType: 'ServicePrincipal'
    tenantId: subscription().tenantId
  }
}

// PostgreSQL Server firewall rule for Container App
resource postgresqlFirewall 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2023-12-01-preview' = {
  parent: postgresqlServer
  name: 'AllowContainerApp'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '255.255.255.255'
  }
}

// Create databases
resource weatherDatabase 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2023-12-01-preview' = {
  parent: postgresqlServer
  name: 'weather'
}

resource productsDatabase 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2023-12-01-preview' = {
  parent: postgresqlServer
  name: 'products'
}

// Azure Key Vault for secrets management
resource keyVault 'Microsoft.KeyVault/vaults@2023-02-01' = {
  name: 'kv-${resourceToken}'
  location: location
  tags: tags
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true
    publicNetworkAccess: 'Enabled'
  }
}

// Store Redis connection string in Key Vault
resource redisConnectionStringSecret 'Microsoft.KeyVault/vaults/secrets@2023-02-01' = {
  parent: keyVault
  name: 'RedisConnectionString'
  properties: {
    value: '${redis.properties.hostName}:${redis.properties.port}?ssl=True&password=${redis.listKeys().primaryKey}'
  }
}

// Store PostgreSQL Weather Database connection string in Key Vault (managed identity auth)
resource postgresqlWeatherConnectionStringSecret 'Microsoft.KeyVault/vaults/secrets@2023-02-01' = {
  parent: keyVault
  name: 'PostgresqlWeatherConnectionString'
  properties: {
    value: 'Server=${postgresqlServer.properties.fullyQualifiedDomainName};Port=5432;Database=weather;User Id=${managedIdentity.properties.principalId};SslMode=Require;Trust Server Certificate=true;'
  }
}

// Store PostgreSQL Products Database connection string in Key Vault (managed identity auth)
resource postgresqlProductsConnectionStringSecret 'Microsoft.KeyVault/vaults/secrets@2023-02-01' = {
  parent: keyVault
  name: 'PostgresqlProductsConnectionString'
  properties: {
    value: 'Server=${postgresqlServer.properties.fullyQualifiedDomainName};Port=5432;Database=products;User Id=${managedIdentity.properties.principalId};SslMode=Require;Trust Server Certificate=true;'
  }
}

// Grant Managed Identity permission to read secrets from Key Vault
// Role ID: 4633458b-17de-408a-b874-0445c86b69e6 = Key Vault Secrets User
resource keyVaultSecretsAccessRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: keyVault
  name: guid(keyVault.id, managedIdentity.id, '4633458b-17de-408a-b874-0445c86b69e6')
  properties: {
    principalId: managedIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
  }
}

// Grant Managed Identity permission to pull images from ACR
// Role ID: 7f951dda-4ed3-4680-a7ca-43fe172d538d = AcrPull
resource acrPullRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: containerRegistry
  name: guid(containerRegistry.id, managedIdentity.id, '7f951dda-4ed3-4680-a7ca-43fe172d538d')
  properties: {
    principalId: managedIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
  }
}

output containerAppsEnvironmentId string = containerAppsEnvironment.id
output containerAppsEnvironmentName string = containerAppsEnvironment.name
output containerRegistryUrl string = containerRegistry.properties.loginServer
output containerRegistryName string = containerRegistry.name
output redisPrimaryConnectionString string = '${redis.properties.hostName}:${redis.properties.port}?ssl=True'
output postgresqlServerName string = postgresqlServer.name
output postgresqlServerFullyQualifiedDomainName string = postgresqlServer.properties.fullyQualifiedDomainName
output postgresqlWeatherDatabaseName string = weatherDatabase.name
output postgresqlProductsDatabaseName string = productsDatabase.name
output logAnalyticsWorkspaceId string = logAnalyticsWorkspace.id
output logAnalyticsWorkspaceName string = logAnalyticsWorkspace.name
output applicationInsightsConnectionString string = applicationInsights.properties.ConnectionString
output applicationInsightsInstrumentationKey string = applicationInsights.properties.InstrumentationKey
output applicationInsightsDashboardId string = applicationInsightsDashboard.id
output keyVaultId string = keyVault.id
output keyVaultName string = keyVault.name
output keyVaultUri string = keyVault.properties.vaultUri
output redisConnectionStringSecretId string = redisConnectionStringSecret.id
output postgresqlWeatherConnectionStringSecretId string = postgresqlWeatherConnectionStringSecret.id
output postgresqlProductsConnectionStringSecretId string = postgresqlProductsConnectionStringSecret.id
output managedIdentityId string = managedIdentity.id
output managedIdentityPrincipalId string = managedIdentity.properties.principalId
output managedIdentityName string = managedIdentity.name
output resourceToken string = resourceToken
