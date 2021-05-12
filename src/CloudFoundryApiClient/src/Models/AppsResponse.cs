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
        public DateTime Created_at { get; set; }
        public DateTime Updated_at { get; set; }
        public Lifecycle Lifecycle { get; set; }
        public Relationships Relationships { get; set; }
        public Links Links { get; set; }
        public Metadata Metadata { get; set; }
    }

    public class Lifecycle
    {
        public string Type { get; set; }
        public Data Data { get; set; }
    }

    public class Data
    {
        public string[] Buildpacks { get; set; }
        public string Stack { get; set; }
    }

    public class Relationships
    {
        public SpaceParent Space { get; set; }
    }

    public class SpaceParent
    {
        public Data1 Data { get; set; }
    }

    public class Data1
    {
        public string Guid { get; set; }
    }

    public class Links
    {
        public HypertextReference Self { get; set; }
        public HypertextReference Environment_variables { get; set; }
        public HypertextReference Space { get; set; }
        public HypertextReference Processes { get; set; }
        public HypertextReference Packages { get; set; }
        public HypertextReference Current_droplet { get; set; }
        public HypertextReference Droplets { get; set; }
        public HypertextReference Tasks { get; set; }
        public Start Start { get; set; }
        public Stop Stop { get; set; }
        public HypertextReference Revisions { get; set; }
        public HypertextReference Deployed_revisions { get; set; }
        public HypertextReference Features { get; set; }
    }

    public class Start
    {
        public string Href { get; set; }
        public string Method { get; set; }
    }

    public class Stop
    {
        public string Href { get; set; }
        public string Method { get; set; }
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
