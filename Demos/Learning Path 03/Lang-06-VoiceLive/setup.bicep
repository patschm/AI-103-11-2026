param accounts_name string = 'voice-live-ps-resource'

resource accounts_name_resource 'Microsoft.CognitiveServices/accounts@2025-06-01' = {
  name: accounts_name
  location: 'swedencentral'
  sku: {
    name: 'S0'
  }
  kind: 'AIServices'
  properties: {
    customSubDomainName: accounts_name
    }
  }
}


