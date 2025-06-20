using Newtonsoft.Json;

namespace WildfireAgents.Models
{
    public class GeoLocation
    {
        [JsonProperty("latitude")]
        public double Latitude { get; set; }
        [JsonProperty("longitude")]
        public double Longitude { get; set; }
    }
}
