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
            if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
            {
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "VivesHogeschoolProject/1.0");
            }
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

                string weatherJson = await _httpClient.GetStringAsync("https://api.open-meteo.com/v1/forecast?latitude=50.85&longitude=4.35&current_weather=true");
                var weatherData = JsonNode.Parse(weatherJson);
                
                double temp = weatherData?["current_weather"]?["temperature"]?.GetValue<double>() ?? 0;
                double windSpeed = weatherData?["current_weather"]?["windspeed"]?.GetValue<double>() ?? 0;

                string emailBody = $@"Geachte lezer,<br><br>
                Hierbij ontvangt u de geautomatiseerde update met actuele data.<br><br>
                <b>WISSELKOERSEN (FOREX)</b><br>
                --------------------------------------------------<br>
                De wisselkoers van de Euro ten opzichte van de Amerikaanse Dollar bedraagt momenteel:<br>
                1 EUR = <b>{usdRate} USD</b><br><br>
                <b>HET WEER (Brussel)</b><br>
                --------------------------------------------------<br>
                - Huidige temperatuur: {temp} °C<br>
                - Windsnelheid: {windSpeed} km/u<br><br>
                --------------------------------------------------<br>
                Rapport gegenereerd op: {DateTime.Now:dd-MM-yyyy HH:mm}<br>
Status: Succesvol verwerkt via Azure Service Bus.";

                string combinedJsonPayload = $@"{{ ""forex_data"": {forexJson}, ""weather_data"": {weatherJson} }}";

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