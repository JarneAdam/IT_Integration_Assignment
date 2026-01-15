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
            _logger.LogInformation($"Bericht ontvangen voor verwerking: {myQueueItem}");

            try 
            {
                var data = JsonNode.Parse(myQueueItem);

                // Data ophalen
                string usdRate = data["currency"]?["rates"]?["USD"]?.ToString() ?? "Niet beschikbaar";

                double btcPrice = (double?)data["crypto"]?["bitcoin"]?["eur"] ?? 0;
                double btcChange = (double?)data["crypto"]?["bitcoin"]?["eur_24h_change"] ?? 0;
                double ethPrice = (double?)data["crypto"]?["ethereum"]?["eur"] ?? 0;

                // Logica voor trend tekst (zonder emoji's)
                string trend = btcChange >= 0 ? "stijging" : "daling";

                // Professionele e-mail opmaak
                string emailBody = $@"Onderwerp: Dagelijks Financieel Marktoverzicht

Geachte lezer,

Hierbij ontvangt u de geautomatiseerde update betreffende de actuele marktkansen.

WISSELKOERSEN (FOREX)
--------------------------------------------------
De wisselkoers van de Euro ten opzichte van de Amerikaanse Dollar bedraagt momenteel:
1 EUR = {usdRate} USD

CRYPTOVALUTA OVERZICHT
--------------------------------------------------
Bitcoin (BTC):
- Huidige waarde: EUR {btcPrice:N2}
- Marktontwikkeling (24u): Een {trend} van {btcChange:N2}%

Ethereum (ETH):
- Huidige waarde: EUR {ethPrice:N2}

--------------------------------------------------
Rapport gegenereerd op: {DateTime.Now:dd-MM-yyyy HH:mm}
Status: Succesvol verwerkt via Azure Service Bus.

Met vriendelijke groet,

Uw Azure Cloud Systeem";

                _logger.LogInformation("E-mail tekst succesvol gegenereerd.");

                return emailBody;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Fout tijdens dataverwerking: {ex.Message}");
                return "Er is een technische fout opgetreden bij het genereren van het rapport.";
            }
        }
    }
}