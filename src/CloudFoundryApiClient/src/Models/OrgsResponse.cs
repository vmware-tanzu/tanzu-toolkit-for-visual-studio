using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Tanzu.Toolkit.CloudFoundryApiClient.Models.OrgsResponse
{
    [DataContract]
    public class OrgsResponse
    {
        public Pagination Pagination { get; set; }
        [JsonPropertyName("resources")]
        public Org[] Orgs { get; set; }
    }

    public class Org
    {
        public string Guid { get; set; }
        public string Name { get; set; }
    }
}
