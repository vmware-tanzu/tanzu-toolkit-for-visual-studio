using System;

namespace Tanzu.Toolkit.Services.CfCli.Models.Spaces
{
    public class SpacesApiV2ResponsePage : ApiV2Response
    {
        public override string next_url { get; set; }
        public override string prev_url { get; set; }
        public override int total_pages { get; set; }
        public override int total_results { get; set; }
        
        public Space[] resources { get; set; }
    }

    public class Space
    {
        public Entity entity { get; set; }
        public Metadata metadata { get; set; }
    }

    public class Entity
    {
        public bool allow_ssh { get; set; }
        public string app_events_url { get; set; }
        public string apps_url { get; set; }
        public string auditors_url { get; set; }
        public string developers_url { get; set; }
        public string domains_url { get; set; }
        public string events_url { get; set; }
        public object isolation_segment_guid { get; set; }
        public string managers_url { get; set; }
        public string name { get; set; }
        public string organization_guid { get; set; }
        public string organization_url { get; set; }
        public string routes_url { get; set; }
        public string security_groups_url { get; set; }
        public string service_instances_url { get; set; }
        public object space_quota_definition_guid { get; set; }
        public string staging_security_groups_url { get; set; }
    }

    public class Metadata
    {
        public DateTime created_at { get; set; }
        public string guid { get; set; }
        public DateTime updated_at { get; set; }
        public string url { get; set; }
    }

}
