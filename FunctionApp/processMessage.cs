using System;
using System.Text.Json.Nodes;
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
        [ServiceBusOutput("sbq-processed", Connection = "ServiceBusConnection")]
        public string Run(
            [ServiceBusTrigger("sbq-messages", Connection = "ServiceBusConnection")] string myQueueItem)
        {
            _logger.LogInformation($"Bericht ontvangen: {myQueueItem}");

            try 
            {
                var data = JsonNode.Parse(myQueueItem);

                string usdRate = data["currency"]?["rates"]?["USD"]?.ToString() ?? "Onbekend";

                double btcPrice = (double?)data["crypto"]?["bitcoin"]?["eur"] ?? 0;
                double btcChange = (double?)data["crypto"]?["bitcoin"]?["eur_24h_change"] ?? 0;
                double ethPrice = (double?)data["crypto"]?["ethereum"]?["eur"] ?? 0;

                string trend = btcChange >= 0 ? "stijging 📈" : "daling 📉";

                string emailBody = $@"Beste,

Hierbij ontvangen jullie de dagelijkse financiële update.

💵 Wisselkoers:
- 1 Euro is momenteel ${usdRate} waard.

🪙 Crypto Markt:
- Bitcoin: €{btcPrice:N2} (24u {trend} van {btcChange:N2}%)
- Ethereum: €{ethPrice:N2}

Met vriendelijke groet,
De Azure Bot";

                _logger.LogInformation($"Gegenereerde tekst: {emailBody}");

                return emailBody;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Fout bij verwerken: {ex.Message}");
                return "Er ging iets mis bij het verwerken van de data.";
            }
        }
    }
}