using System.Text.Json.Serialization;

namespace Tanzu.Toolkit.CloudFoundryApiClient.Models.SpacesResponse
{
    public class SpacesResponse
    {
        [JsonPropertyName("pagination")]
        public Pagination Pagination { get; set; }

        [JsonPropertyName("resources")]
        public Space[] Spaces { get; set; }
    }

    public class Space
    {
        [JsonPropertyName("guid")]
        public string Guid { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
}