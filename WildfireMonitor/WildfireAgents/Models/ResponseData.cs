using Newtonsoft.Json;

namespace WildfireAgents.Models
{
    public class ResponseData
    {
        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        [JsonProperty("regionId")]
        public string RegionId { get; set; }
        [JsonProperty("recommendations")]
        public string Recommendations { get; set; }
        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }
    }
}
