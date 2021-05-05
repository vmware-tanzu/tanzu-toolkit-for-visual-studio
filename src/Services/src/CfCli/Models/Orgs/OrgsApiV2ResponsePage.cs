using System;

namespace Tanzu.Toolkit.Services.CfCli.Models.Orgs
{
    public class OrgsApiV2ResponsePage : ApiV2Response
    {
        public override string next_url { get; set; }
        public override string prev_url { get; set; }
        public override int total_pages { get; set; }
        public override int total_results { get; set; }
        
        public Org[] resources { get; set; }
    }

    public class Org
    {
        public Entity entity { get; set; }
        public Metadata metadata { get; set; }
    }

    public class Entity
    {
        public string app_events_url { get; set; }
        public string auditors_url { get; set; }
        public bool billing_enabled { get; set; }
        public string billing_managers_url { get; set; }
        public object default_isolation_segment_guid { get; set; }
        public string domains_url { get; set; }
        public string managers_url { get; set; }
        public string name { get; set; }
        public string private_domains_url { get; set; }
        public string quota_definition_guid { get; set; }
        public string quota_definition_url { get; set; }
        public string space_quota_definitions_url { get; set; }
        public string spaces_url { get; set; }
        public string status { get; set; }
        public string users_url { get; set; }
    }

    public class Metadata
    {
        public DateTime created_at { get; set; }
        public string guid { get; set; }
        public DateTime updated_at { get; set; }
        public string url { get; set; }
    }

}
