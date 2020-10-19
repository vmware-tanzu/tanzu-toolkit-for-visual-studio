using Newtonsoft.Json;
using System;

namespace TanzuForVS.CloudFoundryApiClient.Models.SpacesResponse
{

    public class SpacesResponse
    {
        public Pagination pagination { get; set; }

        [JsonProperty("resources")]
        public Space[] Spaces { get; set; }
    }

    public class Pagination
    {
        public int total_results { get; set; }
        public int total_pages { get; set; }
        public Href first { get; set; }
        public Href last { get; set; }
        public Href next { get; set; }
        public Href previous { get; set; }
    }


    public class Space
    {
        public string guid { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public string name { get; set; }
        public Relationships relationships { get; set; }
        public Links links { get; set; }
        public Metadata metadata { get; set; }
    }

    public class Relationships
    {
        public Organization organization { get; set; }
        public Quota quota { get; set; }
    }

    public class Organization
    {
        public Data data { get; set; }
    }

    public class Data
    {
        public string guid { get; set; }
    }

    public class Quota
    {
        public object data { get; set; }
    }

    public class Links
    {
        public Href self { get; set; }
        public Href organization { get; set; }
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
