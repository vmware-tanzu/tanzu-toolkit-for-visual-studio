using System;
using System.Text.Json.Serialization;

namespace Tanzu.Toolkit.Services.CfCli.Models.Orgs
{
    public class OrgsApiV2ResponsePage : ApiV2Response
    {
        public override string NextUrl { get; set; }
        public override string PrevUrl { get; set; }
        public override int TotalPages { get; set; }
        public override int TotalResults { get; set; }

        [JsonPropertyName("resources")]
        public Org[] Resources { get; set; }
    }

    public class Org
    {
        [JsonPropertyName("entity")]
        public Entity Entity { get; set; }
        [JsonPropertyName("metadata")]
        public Metadata Metadata { get; set; }
    }

    public class Entity
    {
        [JsonPropertyName("app_events_url")]
        public string AppEventsUrl { get; set; }
        [JsonPropertyName("auditors_url")]
        public string AuditorsUrl { get; set; }
        [JsonPropertyName("billing_enabled")]
        public bool BillingEnabled { get; set; }
        [JsonPropertyName("billing_managers_url")]
        public string BillingManagersUrl { get; set; }
        [JsonPropertyName("default_isolation_segment_guid")]
        public object DefaultIsolationSegmentGuid { get; set; }
        [JsonPropertyName("domains_url")]
        public string DomainsUrl { get; set; }
        [JsonPropertyName("managers_url")]
        public string ManagersUrl { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("private_domains_url")]
        public string PrivateDomainsUrl { get; set; }
        [JsonPropertyName("quota_definition_guid")]
        public string QuotaDefinitionGuid { get; set; }
        [JsonPropertyName("quota_definition_url")]
        public string QuotaDefinitionUrl { get; set; }
        [JsonPropertyName("space_quota_definitions_url")]
        public string SpaceQuotaDefinitionsUrl { get; set; }
        [JsonPropertyName("spaces_url")]
        public string SpacesUrl { get; set; }
        [JsonPropertyName("status")]
        public string Status { get; set; }
        [JsonPropertyName("users_url")]
        public string UsersUrl { get; set; }
    }

    public class Metadata
    {
        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }
        [JsonPropertyName("guid")]
        public string Guid { get; set; }
        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }
        [JsonPropertyName("url")]
        public string Url { get; set; }
    }
}
