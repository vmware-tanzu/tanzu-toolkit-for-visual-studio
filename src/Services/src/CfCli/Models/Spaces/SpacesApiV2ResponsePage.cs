using System;
using System.Text.Json.Serialization;

namespace Tanzu.Toolkit.Services.CfCli.Models.Spaces
{
    public class SpacesApiV2ResponsePage : ApiV2Response
    {
        public override string NextUrl { get; set; }
        public override string PrevUrl { get; set; }
        public override int TotalPages { get; set; }
        public override int TotalResults { get; set; }

        [JsonPropertyName("resources")]
        public Space[] Resources { get; set; }
    }

    public class Space
    {
        [JsonPropertyName("entity")]
        public Entity Entity { get; set; }
        [JsonPropertyName("metadata")]
        public Metadata Metadata { get; set; }
    }

    public class Entity
    {
        [JsonPropertyName("allow_ssh")]
        public bool AllowSsh { get; set; }
        [JsonPropertyName("app_events_url")]
        public string AppEventsUrl { get; set; }
        [JsonPropertyName("apps_url")]
        public string AppsUrl { get; set; }
        [JsonPropertyName("auditors_url")]
        public string AuditorsUrl { get; set; }
        [JsonPropertyName("developers_url")]
        public string DevelopersUrl { get; set; }
        [JsonPropertyName("domains_url")]
        public string DomainsUrl { get; set; }
        [JsonPropertyName("events_url")]
        public string EventsUrl { get; set; }
        [JsonPropertyName("isolation_segment_guid")]
        public object IsolationSegmentGuid { get; set; }
        [JsonPropertyName("managers_url")]
        public string ManagersUrl { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("organization_guid")]
        public string OrganizationGuid { get; set; }
        [JsonPropertyName("organization_url")]
        public string OrganizationUrl { get; set; }
        [JsonPropertyName("routes_url")]
        public string RoutesUrl { get; set; }
        [JsonPropertyName("security_groups_url")]
        public string SecurityGroupsUrl { get; set; }
        [JsonPropertyName("service_instances_url")]
        public string ServiceInstancesUrl { get; set; }
        [JsonPropertyName("space_quota_definition_guid")]
        public object SpaceQuotaDefinitionGuid { get; set; }
        [JsonPropertyName("staging_security_groups_url")]
        public string StagingSecurityGroupsUrl { get; set; }
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
