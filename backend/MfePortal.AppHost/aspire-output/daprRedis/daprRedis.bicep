@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource daprRedis 'Microsoft.Cache/redisEnterprise@2025-07-01' = {
  name: take('daprRedis-${uniqueString(resourceGroup().id)}', 60)
  location: location
  sku: {
    name: 'Balanced_B0'
  }
  properties: {
    minimumTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

resource daprRedis_default 'Microsoft.Cache/redisEnterprise/databases@2025-07-01' = {
  name: 'default'
  properties: {
    accessKeysAuthentication: 'Disabled'
    port: 10000
  }
  parent: daprRedis
}

output connectionString string = '${daprRedis.properties.hostName}:10000,ssl=true'

output name string = daprRedis.name

output id string = daprRedis.id

output hostName string = daprRedis.properties.hostName