using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Company.Function
{
    public class ProcessMessage
    {
        private readonly ILogger<ProcessMessage> _logger;

        public ProcessMessage(ILogger<ProcessMessage> logger)
        {
            _logger = logger;
        }

        [Function(nameof(ProcessMessage))]
        // OUTPUT: Sends the result to the second queue 'sbq-processed'
        [ServiceBusOutput("sbq-processed", Connection = "ServiceBusConnection")]
        public string Run(
            // TRIGGER: Listens to the first queue 'sbq-messages'
            [ServiceBusTrigger("sbq-messages", Connection = "ServiceBusConnection")] string myQueueItem)
        {
            _logger.LogInformation($"C# ServiceBus queue trigger function processed message: {myQueueItem}");

            // Logic: Add a timestamp to the message
            var result = $"{myQueueItem} | Processed at: {DateTime.Now}";

            return result;
        }
    }
}