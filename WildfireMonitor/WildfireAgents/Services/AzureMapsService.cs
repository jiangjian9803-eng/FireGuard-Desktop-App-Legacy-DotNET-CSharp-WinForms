using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace WildfireAgents.Services
{
    public class AzureMapsService
    {
        private readonly HttpClient _httpClient;
        private readonly KeyVaultService _keyVaultService;

        public AzureMapsService(HttpClient httpClient, IConfiguration configuration, KeyVaultService keyVaultService)
        {
            _httpClient = httpClient;
            _keyVaultService = keyVaultService;
        }

        public async Task<DailyWeatherResponse> GetDailyWeatherAsync(double latitude, double longitude)
        {
            var _mapsKey = await _keyVaultService.GetSecretAsync("AzureMapsKey");
            var latitudeStr = latitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var longitudeStr = longitude.ToString(System.Globalization.CultureInfo.InvariantCulture);

            var url = $"https://atlas.microsoft.com/weather/forecast/daily/json?query={latitudeStr},{longitudeStr}&api-version=1.0&subscription-key={_mapsKey}&duration=1";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var forecast = doc.RootElement.GetProperty("forecasts")[0];

            var temperature = forecast.GetProperty("temperature").GetProperty("maximum").GetProperty("value").GetDouble();
            var wind = forecast.GetProperty("day").GetProperty("wind");
            var windSpeed = wind.GetProperty("speed").GetProperty("value").GetDouble();
            var windDirection = wind.GetProperty("direction").GetProperty("degrees").GetDouble();
            var precipitationProbability = forecast.GetProperty("day").GetProperty("precipitationProbability").GetDouble();

            return new DailyWeatherResponse
            {
                Temperature = temperature,
                WindSpeed = windSpeed,
                WindDirection = windDirection,
                PrecipitationProbability = precipitationProbability
            };
        }
        
        public class DailyWeatherResponse
        {
            public double Temperature { get; set; }
            public double WindSpeed { get; set; }
            public double WindDirection { get; set; }
            public double PrecipitationProbability { get; set; }
        }
    }
}
