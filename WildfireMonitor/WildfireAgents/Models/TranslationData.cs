using Newtonsoft.Json;

namespace WildfireAgents.Models
{
    public class TranslationData
    {
        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        [JsonProperty("regionId")]
        public string RegionId { get; set; }
        [JsonProperty("language")]
        public string Language { get; set; }
        [JsonProperty("translatedSummary")]
        public string TranslatedSummary { get; set; }
        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }
    }
}
