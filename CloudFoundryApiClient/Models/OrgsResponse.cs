﻿using Newtonsoft.Json;
using System;

namespace Tanzu.Toolkit.CloudFoundryApiClient.Models.OrgsResponse
{
    public class OrgsResponse
    {
        public Pagination pagination { get; set; }
        [JsonProperty("resources")]
        public Org[] Orgs { get; set; }
    }

    public class Org
    {
        public string guid { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public string name { get; set; }
        public bool suspended { get; set; }
        public Relationships relationships { get; set; }
        public Links links { get; set; }
        public Metadata metadata { get; set; }
    }

    public class Relationships
    {
        public Quota quota { get; set; }
    }

    public class Quota
    {
        public Data data { get; set; }
    }

    public class Data
    {
        public string guid { get; set; }
    }

    public class Links
    {
        public Href self { get; set; }
        public Href domains { get; set; }
        public Href default_domain { get; set; }
        public Href quota { get; set; }
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
