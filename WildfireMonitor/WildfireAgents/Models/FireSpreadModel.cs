using Newtonsoft.Json;

namespace WildfireAgents.Models
{
    public class FireSpreadModel
    {
        [JsonProperty("path")]
        public string Path { get; set; }
        [JsonProperty("riskLevel")]
        public double RiskLevel { get; set; }
    }
}
