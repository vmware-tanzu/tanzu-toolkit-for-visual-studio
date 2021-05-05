using System;

namespace Tanzu.Toolkit.Services.CfCli.Models.Apps
{
    public class AppsApiV2Response
    {
        public string guid { get; set; }
        public string name { get; set; }
        public App[] apps { get; set; }
        public object[] services { get; set; }
    }

    public class App
    {
        public string guid { get; set; }
        public string[] urls { get; set; }
        public Route[] routes { get; set; }
        public int service_count { get; set; }
        public object[] service_names { get; set; }
        public int running_instances { get; set; }
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
        public DateTime package_updated_at { get; set; }
        public string detected_start_command { get; set; }
        public bool enable_ssh { get; set; }
        public object ports { get; set; }
    }

    public class Environment_Json
    {
    }

    public class Route
    {
        public string guid { get; set; }
        public string host { get; set; }
        public object port { get; set; }
        public string path { get; set; }
        public Domain domain { get; set; }
    }

    public class Domain
    {
        public string guid { get; set; }
        public string name { get; set; }
    }

}