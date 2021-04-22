using System;
using System.Runtime.Serialization;

namespace Tanzu.Toolkit.CloudFoundryApiClient.Models.OrgsResponse
{
    [DataContract]
    public class OrgsResponse
    {
        [DataMember(Name = "pagination")]
        public Pagination Pagination { get; set; }
        [DataMember(Name = "resources")]
        public Org[] Orgs { get; set; }
    }

    public class Org
    {
        [DataMember(Name = "guid")]
        public string Guid { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        [DataMember(Name = "name")]
        public string Name { get; set; }
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
