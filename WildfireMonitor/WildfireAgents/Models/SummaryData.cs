using Newtonsoft.Json;

namespace WildfireAgents.Models
{
    public class SummaryData
    {
        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        [JsonProperty("regionId")]
        public string RegionId { get; set; }
        [JsonProperty("summary")]
        public string Summary { get; set; }
        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }
    }
}
