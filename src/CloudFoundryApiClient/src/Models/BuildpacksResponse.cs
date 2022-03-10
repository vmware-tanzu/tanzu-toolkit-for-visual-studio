using System.Text.Json.Serialization;

namespace Tanzu.Toolkit.CloudFoundryApiClient.Models
{
    public class BuildpacksResponse
    {
        [JsonPropertyName("pagination")]
        public Pagination Pagination { get; set; }

        [JsonPropertyName("resources")]
        public Buildpack[] Buildpacks { get; set; }
    }

    public class Buildpack
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("stack")]
        public string Stack { get; set; }
    }
}
