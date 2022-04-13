using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Tanzu.Toolkit.CloudFoundryApiClient.Models
{
    public class ServicesResponse
    {
        [JsonPropertyName("pagination")]
        public Pagination Pagination { get; set; }

        [JsonPropertyName("resources")]
        public Service[] Services { get; set; }
    }

    public class Service
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

}
