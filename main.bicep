@description('Location for all resources')
param location string = 'northeurope'

@description('First name for resource naming')
param firstName string

@description('Last name for resource naming')
param lastName string

// Service Bus Namespace
resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
  name: 'sb-${firstName}-${lastName}'
  location: location
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
}
