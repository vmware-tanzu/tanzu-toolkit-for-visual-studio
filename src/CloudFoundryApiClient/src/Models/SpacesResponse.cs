using System;
using Newtonsoft.Json;

namespace Tanzu.Toolkit.CloudFoundryApiClient.Models.SpacesResponse
{
    public class SpacesResponse
    {
        [JsonProperty("pagination")]
        public Pagination Pagination { get; set; }

        [JsonProperty("resources")]
        public Space[] Spaces { get; set; }
    }

    public class Space
    {
        [JsonProperty("guid")]
        public string Guid { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
