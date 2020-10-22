using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace TanzuForVS.CloudFoundryApiClient.Models.AppsResponse
{

    public class AppsResponse
    {
        public Pagination pagination { get; set; }

        [JsonProperty("resources")]
        public App[] Apps { get; set; }
    }

    public class App
    {
        public string guid { get; set; }
        public string name { get; set; }
        public string state { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public Lifecycle lifecycle { get; set; }
        public Relationships relationships { get; set; }
        public Links links { get; set; }
        public Metadata metadata { get; set; }
    }

    public class Lifecycle
    {
        public string type { get; set; }
        public Data data { get; set; }
    }

    public class Data
    {
        public string[] buildpacks { get; set; }
        public string stack { get; set; }
    }

    public class Relationships
    {
        [JsonProperty("spaces")]
        public SpaceParent space { get; set; }
    }

    public class SpaceParent
    {
        public Data1 data { get; set; }
    }

    public class Data1
    {
        public string guid { get; set; }
    }

    public class Links
    {
        public Href self { get; set; }
        public Href environment_variables { get; set; }
        public Href space { get; set; }
        public Href processes { get; set; }
        public Href packages { get; set; }
        public Href current_droplet { get; set; }
        public Href droplets { get; set; }
        public Href tasks { get; set; }
        public Start start { get; set; }
        public Stop stop { get; set; }
        public Href revisions { get; set; }
        public Href deployed_revisions { get; set; }
        public Href features { get; set; }
    }
    public class Start
    {
        public string href { get; set; }
        public string method { get; set; }
    }

    public class Stop
    {
        public string href { get; set; }
        public string method { get; set; }
    }

    public class Metadata
    {
        public Labels labels { get; set; }
        public Annotations annotations { get; set; }
    }

    public class Labels
    {
    }

    public class Annotations
    {
    }

}
