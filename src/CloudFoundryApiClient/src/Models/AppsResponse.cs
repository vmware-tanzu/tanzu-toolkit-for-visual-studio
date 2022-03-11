using System.Text.Json.Serialization;

namespace Tanzu.Toolkit.CloudFoundryApiClient.Models.AppsResponse
{
    public class AppsResponse
    {
        [JsonPropertyName("pagination")]
        public Pagination Pagination { get; set; }

        [JsonPropertyName("resources")]
        public App[] Apps { get; set; }
    }

    public class App
    {
        [JsonPropertyName("guid")]
        public string Guid { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("state")]
        public string State { get; set; }
    }
}
