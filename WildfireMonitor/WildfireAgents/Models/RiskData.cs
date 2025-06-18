using Newtonsoft.Json;

namespace WildfireAgents.Models
{
    public class RiskData
    {
        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        [JsonProperty("regionId")]
        public string RegionId { get; set; }
        [JsonProperty("riskScore")]
        public double RiskScore { get; set; }
        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }
    }
}
