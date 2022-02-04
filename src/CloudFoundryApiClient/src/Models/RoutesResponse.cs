using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Tanzu.Toolkit.CloudFoundryApiClient.Models
{
    public class RoutesResponse
    {
        [JsonProperty("pagination")]
        public Pagination Pagination { get; set; }

        [JsonProperty("resources")]
        public Route[] Routes { get; set; }
    }

    public class Route
    {
        [JsonProperty("guid")]
        public string Guid { get; set; }
    }
}
