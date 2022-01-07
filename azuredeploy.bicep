@description('The base name for resources')
param name string = uniqueString(resourceGroup().id)

@description('The location for resources')
param location string = resourceGroup().location

@description('A secret to use in the web application')
@secure()
param secret string = newGuid()

@description('The web site hosting plan')
@allowed([
  'F1'
  'B1'
  'B2'
  'B3'
  'S1'
  'S2'
  'S3'
])
param sku string = 'F1'

@description('The App Configuration SKU. Only "standard" supports customer-managed keys from Key Vault')
@allowed([
  'free'
  'standard'
])
param configSku string = 'standard'

@description('Optional additional principal ID to grant read access to resources')
param principalId string = ''

@description('Principal type for the optional "principalId" parameter')
@allowed([
  'ForeignGroup'
  'Group'
  'ServicePrincipal'
  'User'
])
param principalType string = 'ServicePrincipal'

resource config 'Microsoft.AppConfiguration/configurationStores@2020-06-01' = {
  name: '${name}config'
  location: location
  sku: {
    name: configSku
  }

  resource configValue 'keyValues@2020-07-01-preview' = {
    name: 'SampleValue'
    properties: {
      contentType: 'text/plain'
      value: 'not a secret'
    }
  }
}

resource kv 'Microsoft.KeyVault/vaults@2019-09-01' = {
  // Make sure the Key Vault name begins with a letter.
  name: 'kv${name}'
  location: location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true
  }

  resource kvSecret 'secrets@2019-09-01' = {
    name: 'SampleSecret'
    properties: {
      contentType: 'text/plain'
      value: secret
    }
  }
}

resource plan 'Microsoft.Web/serverfarms@2020-12-01' = {
  name: '${name}plan'
  location: location
  sku: {
    name: sku
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

resource web 'Microsoft.Web/sites@2020-12-01' = {
  name: '${name}web'
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    httpsOnly: true
    serverFarmId: plan.id
    siteConfig: {
      linuxFxVersion: 'DOTNET|6.0'
      connectionStrings: [
        {
          name: 'AppConfig'
          connectionString: listKeys(config.id, config.apiVersion).value[0].connectionString
        }
      ]
    }
  }

  resource appSettings 'config@2020-12-01' = {
    name: 'appsettings'
    properties: {
      APPCONFIG_URI: config.properties.endpoint
      KEYVAULT_URI: kv.properties.vaultUri
    }
  }
}

var configDataReaderDefinitionId = '516239f1-63e1-4d78-a4de-a74fb236a071'
resource configDataReaderDefinition 'Microsoft.Authorization/roleDefinitions@2018-01-01-preview' existing = {
  scope: config
  name: configDataReaderDefinitionId
}

resource configDataReaderAssignment 'Microsoft.Authorization/roleAssignments@2020-04-01-preview' = if (principalId != '') {
  name: guid(subscription().id, resourceGroup().id, principalId, configDataReaderDefinitionId)
  scope: config
  properties: {
    roleDefinitionId: configDataReaderDefinition.id
    principalId: principalId
    principalType: principalType
  }
}

resource configWebDataReaderAssignment 'Microsoft.Authorization/roleAssignments@2020-04-01-preview' = {
  name: guid(subscription().id, resourceGroup().id, 'web', configDataReaderDefinitionId)
  scope: config
  properties: {
    roleDefinitionId: configDataReaderDefinition.id
    principalId: web.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

var kvSecretsUserDefinitionId = '4633458b-17de-408a-b874-0445c86b69e6'
resource kvSecretsUserDefinition 'Microsoft.Authorization/roleDefinitions@2018-01-01-preview' existing = {
  scope: config
  name: kvSecretsUserDefinitionId
}

resource kvSecretsUserAssignment 'Microsoft.Authorization/roleAssignments@2020-04-01-preview' = if (principalId != '') {
  name: guid(subscription().id, resourceGroup().id, principalId, kvSecretsUserDefinitionId)
  scope: kv
  properties: {
    roleDefinitionId: kvSecretsUserDefinition.id
    principalId: principalId
    principalType: principalType
  }
}

resource kvWebSecretsUserAssignment 'Microsoft.Authorization/roleAssignments@2020-04-01-preview' = {
  name: guid(subscription().id, resourceGroup().id, 'web', kvSecretsUserDefinitionId)
  scope: kv
  properties: {
    roleDefinitionId: kvSecretsUserDefinition.id
    principalId: web.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

output appConfigUri string = config.properties.endpoint
output keyVaultUri string = kv.properties.vaultUri
output siteUri string = 'https://${web.properties.defaultHostName}/'
