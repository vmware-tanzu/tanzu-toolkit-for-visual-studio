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
        public DateTime Created_at { get; set; }
        public DateTime Updated_at { get; set; }
        [DataMember(Name = "name")]
        public string Name { get; set; }
        public bool Suspended { get; set; }
        public Relationships Relationships { get; set; }
        public Links Links { get; set; }
        public Metadata Metadata { get; set; }
    }

    public class Relationships
    {
        public Quota Quota { get; set; }
    }

    public class Quota
    {
        public Data Data { get; set; }
    }

    public class Data
    {
        public string Guid { get; set; }
    }

    public class Links
    {
        public HypertextReference Self { get; set; }
        public HypertextReference Domains { get; set; }
        public HypertextReference Default_domain { get; set; }
        public HypertextReference Quota { get; set; }
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
