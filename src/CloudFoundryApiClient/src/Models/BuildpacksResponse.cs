using Newtonsoft.Json;
using System;

namespace Tanzu.Toolkit.CloudFoundryApiClient.Models
{
    public class BuildpacksResponse
    {
        [JsonProperty("pagination")]
        public Pagination Pagination { get; set; }

        [JsonProperty("resources")]
        public Buildpack[] Buildpacks { get; set; }
    }

    public class Buildpack
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("stack")]
        public string Stack { get; set; }
    }
}
