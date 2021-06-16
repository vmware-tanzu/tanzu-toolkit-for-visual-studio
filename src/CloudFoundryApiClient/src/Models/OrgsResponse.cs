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
        [DataMember(Name = "name")]
        public string Name { get; set; }
    }
}
