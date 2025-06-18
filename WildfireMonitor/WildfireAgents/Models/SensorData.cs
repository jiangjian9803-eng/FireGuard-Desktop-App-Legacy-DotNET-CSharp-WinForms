using Newtonsoft.Json;

namespace WildfireAgents.Models
{
    public class SensorData
    {
        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        [JsonProperty("regionId")]
        public string RegionId { get; set; }
        [JsonProperty("temperature")]
        public double Temperature { get; set; }
        [JsonProperty("humidity")]
        public double Humidity { get; set; }
        [JsonProperty("windSpeed")]
        public double WindSpeed { get; set; }
        [JsonProperty("location")]
        public GeoLocation Location { get; set; }
        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }
    }

}
