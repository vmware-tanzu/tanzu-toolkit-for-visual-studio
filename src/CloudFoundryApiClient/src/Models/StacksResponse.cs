using Newtonsoft.Json;

namespace Tanzu.Toolkit.CloudFoundryApiClient.Models.StacksResponse
{
    public class StacksResponse
    {
        [JsonProperty("pagination")]
        public Pagination Pagination { get; set; }

        [JsonProperty("resources")]
        public Stack[] Stacks { get; set; }
    }


    public class Stack
    {
        [JsonProperty("guid")]
        public string Guid { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

    }
}

