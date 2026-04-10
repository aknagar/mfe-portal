@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param daprredis_outputs_name string

param principalId string

resource daprRedis 'Microsoft.Cache/redisEnterprise@2025-07-01' existing = {
  name: daprredis_outputs_name
}

resource daprRedis_default 'Microsoft.Cache/redisEnterprise/databases@2025-07-01' existing = {
  name: 'default'
  parent: daprRedis
}

resource daprRedis_default_contributor 'Microsoft.Cache/redisEnterprise/databases/accessPolicyAssignments@2025-07-01' = {
  name: guid(daprRedis_default.id, principalId, 'default')
  properties: {
    accessPolicyName: 'default'
    user: {
      objectId: principalId
    }
  }
  parent: daprRedis_default
}