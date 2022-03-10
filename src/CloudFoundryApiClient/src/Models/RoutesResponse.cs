using System.Text.Json.Serialization;

namespace Tanzu.Toolkit.CloudFoundryApiClient.Models
{
    public class RoutesResponse
    {
        [JsonPropertyName("pagination")]
        public Pagination Pagination { get; set; }

        [JsonPropertyName("resources")]
        public Route[] Routes { get; set; }
    }

    public class Route
    {
        [JsonPropertyName("guid")]
        public string Guid { get; set; }
    }
}
