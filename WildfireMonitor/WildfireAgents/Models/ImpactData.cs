using Newtonsoft.Json;

namespace WildfireAgents.Models
{
    public class ImpactData
    {
        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        [JsonProperty("regionId")]
        public string RegionId { get; set; }
        [JsonProperty("impactedAreas")]
        public string ImpactedAreas { get; set; }
        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }
    }
}
