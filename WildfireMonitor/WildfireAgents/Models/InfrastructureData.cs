using Newtonsoft.Json;

namespace WildfireAgents.Models
{
    public class InfrastructureData
    {
        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        [JsonProperty("regionId")]
        public string RegionId { get; set; }
        [JsonProperty("infrastructureDetails")]
        public string InfrastructureDetails { get; set; }
    }
}
