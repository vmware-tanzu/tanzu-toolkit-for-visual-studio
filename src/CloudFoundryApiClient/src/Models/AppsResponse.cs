using System;
using Newtonsoft.Json;

namespace Tanzu.Toolkit.CloudFoundryApiClient.Models.AppsResponse
{
    public class AppsResponse
    {
        [JsonProperty("pagination")]
        public Pagination Pagination { get; set; }

        [JsonProperty("resources")]
        public App[] Apps { get; set; }
    }

    public class App
    {
        [JsonProperty("guid")]
        public string Guid { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("state")]
        public string State { get; set; }
    }
}
