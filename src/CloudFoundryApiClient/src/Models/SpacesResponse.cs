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
        public DateTime Created_at { get; set; }
        public DateTime Updated_at { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        public Relationships Relationships { get; set; }
        public Links Links { get; set; }
        public Metadata Metadata { get; set; }
    }

    public class Relationships
    {
        public Organization Organization { get; set; }
        public Quota Quota { get; set; }
    }

    public class Organization
    {
        public Data Data { get; set; }
    }

    public class Data
    {
        public string Guid { get; set; }
    }

    public class Quota
    {
        public object Data { get; set; }
    }

    public class Links
    {
        public HypertextReference Self { get; set; }
        public HypertextReference Organization { get; set; }
    }

    public class Metadata
    {
        public Labels Labels { get; set; }
        public Annotations Annotations { get; set; }
    }

    public class Labels
    {
    }

    public class Annotations
    {
    }
}
