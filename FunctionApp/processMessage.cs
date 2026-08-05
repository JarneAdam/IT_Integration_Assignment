using System;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Company.Function
{
    public class MyOutputType
    {
        [ServiceBusOutput("sbq-messages", Connection = "ServiceBusConnection")]
        public string? ServiceBusMessage { get; set; } = null;

        [BlobOutput("messages/payload-{DateTime.UtcNow:yyyy-MM-dd}.json", Connection = "AzureWebJobsStorage")]
        public string? BlobContent { get; set; } = null;
    }

    public class ProcessMessage
    {
        private readonly ILogger<ProcessMessage> _logger;
        private static readonly HttpClient _httpClient = new HttpClient(); 

        public ProcessMessage(ILogger<ProcessMessage> logger)
        {
            _logger = logger;
        }

        [Function("GenerateDailyUpdate")]
        public async Task<MyOutputType> Run([TimerTrigger("0 0 8 * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation($"Dagelijkse update gestart om: {DateTime.Now}");

            try
            {
                string forexJson = await _httpClient.GetStringAsync("https://api.frankfurter.app/latest?from=EUR&to=USD");
                var forexData = JsonNode.Parse(forexJson);

                string usdRate = forexData?["rates"]?["USD"]?.ToString() ?? "Onbekend";

                string cryptoJson = await _httpClient.GetStringAsync("https://api.coingecko.com/api/v3/simple/price?ids=bitcoin,ethereum&vs_currencies=eur&include_24hr_change=true");
                var cryptoData = JsonNode.Parse(cryptoJson);

                double btcPrice = cryptoData?["bitcoin"]?["eur"]?.GetValue<double>() ?? 0;
                double btcChange = cryptoData?["bitcoin"]?["eur_24h_change"]?.GetValue<double>() ?? 0;
                double ethPrice = cryptoData?["ethereum"]?["eur"]?.GetValue<double>() ?? 0;

                string trend = btcChange >= 0 ? "stijging" : "daling";

                string emailBody = $@"Geachte lezer,<br><br>
                Hierbij ontvangt u de geautomatiseerde update betreffende de actuele marktkansen.<br><br>
                <b>WISSELKOERSEN (FOREX)</b><br>
                --------------------------------------------------<br>
                De wisselkoers van de Euro ten opzichte van de Amerikaanse Dollar bedraagt momenteel:<br>
                1 EUR = <b>{usdRate} USD</b><br><br>
                <b>CRYPTOVALUTA OVERZICHT</b><br>
                --------------------------------------------------<br>
                <b>Bitcoin (BTC):</b><br>
                - Huidige waarde: EUR {btcPrice:N2}<br>
                - Marktontwikkeling (24u): Een {trend} van {btcChange:N2}%<br><br>
                <b>Ethereum (ETH):</b><br>
                - Huidige waarde: EUR {ethPrice:N2}<br><br>
                --------------------------------------------------<br>
                Rapport gegenereerd op: {DateTime.Now:dd-MM-yyyy HH:mm}<br>
                Status: Succesvol verwerkt via Azure Service Bus.";

                string combinedJsonPayload = $@"{{ ""forex_data"": {forexJson}, ""crypto_data"": {cryptoJson} }}";

                return new MyOutputType
                {
                    ServiceBusMessage = emailBody,
                    BlobContent = combinedJsonPayload
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Fout bij ophalen API data: {ex.Message}");
                throw; 
            }
        }
    }
}