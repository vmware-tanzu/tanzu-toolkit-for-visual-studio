using System.Runtime.Serialization;

namespace Tanzu.Toolkit.CloudFoundryApiClient.Models.StacksResponse
{
    public class StacksResponse
    {
        [DataMember(Name = "pagination")]
        public Pagination Pagination { get; set; }

        [DataMember(Name = "resources")]
        public Stack[] Stacks { get; set; }
    }


    public class Stack
    {
        [DataMember(Name = "guid")]
        public string Guid { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }

    }
}

