using Newtonsoft.Json;

namespace Tanzu.Toolkit.CloudFoundryApiClient.Models
{
    public class HypertextReference
    {
        [JsonProperty("href")]
        public string Href { get; set; }
    }
}
