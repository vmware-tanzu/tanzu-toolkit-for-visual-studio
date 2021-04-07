using System;

namespace Tanzu.Toolkit.VisualStudio.Services.CfCli.Models.Apps
{
    public class AppsApiV2Response : ApiV2Response
    {
        public override int total_results { get; set; }
        public override int total_pages { get; set; }
        public override string prev_url { get; set; }
        public override string next_url { get; set; }

        public App[] resources { get; set; }
    }

    public class App
    {
        public Metadata metadata { get; set; }
        public Entity entity { get; set; }
    }

    public class Metadata
    {
        public string guid { get; set; }
        public string url { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }

    public class Entity
    {
        public string name { get; set; }
        public bool production { get; set; }
        public string space_guid { get; set; }
        public string stack_guid { get; set; }
        public object buildpack { get; set; }
        public string detected_buildpack { get; set; }
        public string detected_buildpack_guid { get; set; }
        public Environment_Json environment_json { get; set; }
        public int memory { get; set; }
        public int instances { get; set; }
        public int disk_quota { get; set; }
        public string state { get; set; }
        public string version { get; set; }
        public object command { get; set; }
        public bool console { get; set; }
        public object debug { get; set; }
        public string staging_task_id { get; set; }
        public string package_state { get; set; }
        public string health_check_type { get; set; }
        public object health_check_timeout { get; set; }
        public object health_check_http_endpoint { get; set; }
        public object staging_failed_reason { get; set; }
        public object staging_failed_description { get; set; }
        public bool diego { get; set; }
        public object docker_image { get; set; }
        public Docker_Credentials docker_credentials { get; set; }
        public DateTime package_updated_at { get; set; }
        public string detected_start_command { get; set; }
        public bool enable_ssh { get; set; }
        public int[] ports { get; set; }
        public string space_url { get; set; }
        public string stack_url { get; set; }
        public string routes_url { get; set; }
        public string events_url { get; set; }
        public string service_bindings_url { get; set; }
        public string route_mappings_url { get; set; }
    }

    public class Environment_Json
    {
    }

    public class Docker_Credentials
    {
        public object username { get; set; }
        public object password { get; set; }
    }


}
