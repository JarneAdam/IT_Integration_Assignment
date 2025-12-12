@description('Location for all resources')
param location string = 'northeurope'

@description('First name for resource naming')
param firstName string

@description('Last name for resource naming')
param lastName string

// 1. Service Bus Namespace
resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
  name: 'sb-${firstName}-${lastName}'
  location: location
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
}

// 2. Service Bus Queue (The "first" queue for the Logic App)
resource serviceBusQueue 'Microsoft.ServiceBus/namespaces/queues@2022-10-01-preview' = {
  parent: serviceBusNamespace
  name: 'sbq-messages' 
  properties: {
    // Basic settings suitable for the assignment
    lockDuration: 'PT5M'
    maxSizeInMegabytes: 1024
    requiresDuplicateDetection: false
    requiresSession: false
    defaultMessageTimeToLive: 'P14D' // Messages live for 14 days
    deadLetteringOnMessageExpiration: false
    maxDeliveryCount: 10
  }
}