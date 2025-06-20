using Newtonsoft.Json;

namespace WildfireAgents.Models
{
    public class ImpactModel
    {
        [JsonProperty("areas")]
        public string Areas { get; set; }
    }
}
