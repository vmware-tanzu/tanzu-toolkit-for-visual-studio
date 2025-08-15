using System.Text.Json.Serialization;

namespace Tanzu.Toolkit.CloudFoundryApiClient.Models.StacksResponse
{
    public class StacksResponse
    {
        [JsonPropertyName("pagination")]
        public Pagination Pagination { get; set; }

        [JsonPropertyName("resources")]
        public Stack[] Stacks { get; set; }
    }


    public class Stack
    {
        [JsonPropertyName("guid")]
        public string Guid { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
}