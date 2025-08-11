using System.Text.Json.Serialization;

namespace Tanzu.Toolkit.CloudFoundryApiClient.Models
{
    public class HypertextReference
    {
        [JsonPropertyName("href")]
        public string Href { get; set; }

        public override string ToString() => Href.ToString();
    }
}