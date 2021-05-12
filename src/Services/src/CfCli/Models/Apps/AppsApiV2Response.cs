using System;
using System.Text.Json.Serialization;

namespace Tanzu.Toolkit.Services.CfCli.Models.Apps
{
    public class AppsApiV2Response
    {
        [JsonPropertyName("guid")]
        public string Guid { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("apps")]
        public App[] Apps { get; set; }
        [JsonPropertyName("services")]
        public object[] Services { get; set; }
    }

    public class App
    {
        [JsonPropertyName("guid")]
        public string Guid { get; set; }
        [JsonPropertyName("urls")]
        public string[] Urls { get; set; }
        [JsonPropertyName("routes")]
        public Route[] Routes { get; set; }
        [JsonPropertyName("service_count")]
        public int Service_count { get; set; }
        [JsonPropertyName("service_names")]
        public object[] Service_names { get; set; }
        [JsonPropertyName("running_instances")]
        public int Running_instances { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("production")]
        public bool Production { get; set; }
        [JsonPropertyName("space_guid")]
        public string Space_guid { get; set; }
        [JsonPropertyName("stack_guid")]
        public string Stack_guid { get; set; }
        [JsonPropertyName("buildpack")]
        public object Buildpack { get; set; }
        [JsonPropertyName("detected_buildpack")]
        public string Detected_buildpack { get; set; }
        [JsonPropertyName("detected_buildpack_guid")]
        public string Detected_buildpack_guid { get; set; }
        [JsonPropertyName("environment_json")]
        public Environment_Json Environment_json { get; set; }
        [JsonPropertyName("memory")]
        public int Memory { get; set; }
        [JsonPropertyName("instances")]
        public int Instances { get; set; }
        [JsonPropertyName("disk_quota")]
        public int Disk_quota { get; set; }
        [JsonPropertyName("state")]
        public string State { get; set; }
        [JsonPropertyName("version")]
        public string Version { get; set; }
        [JsonPropertyName("command")]
        public object Command { get; set; }
        [JsonPropertyName("console")]
        public bool Console { get; set; }
        [JsonPropertyName("debug")]
        public object Debug { get; set; }
        [JsonPropertyName("staging_task_id")]
        public string Staging_task_id { get; set; }
        [JsonPropertyName("package_state")]
        public string Package_state { get; set; }
        [JsonPropertyName("health_check_type")]
        public string Health_check_type { get; set; }
        [JsonPropertyName("health_check_timeout")]
        public object Health_check_timeout { get; set; }
        [JsonPropertyName("health_check_http_endpoint")]
        public object Health_check_http_endpoint { get; set; }
        [JsonPropertyName("staging_failed_reason")]
        public object Staging_failed_reason { get; set; }
        [JsonPropertyName("staging_failed_description")]
        public object Staging_failed_description { get; set; }
        [JsonPropertyName("diego")]
        public bool Diego { get; set; }
        [JsonPropertyName("docker_image")]
        public object Docker_image { get; set; }
        [JsonPropertyName("package_updated_at")]
        public DateTime Package_updated_at { get; set; }
        [JsonPropertyName("detected_start_command")]
        public string Detected_start_command { get; set; }
        [JsonPropertyName("enable_ssh")]
        public bool Enable_ssh { get; set; }
        [JsonPropertyName("ports")]
        public object Ports { get; set; }
    }

    public class Environment_Json
    {
    }

    public class Route
    {
        [JsonPropertyName("guid")]
        public string Guid { get; set; }
        [JsonPropertyName("host")]
        public string Host { get; set; }
        [JsonPropertyName("port")]
        public object Port { get; set; }
        [JsonPropertyName("path")]
        public string Path { get; set; }
        [JsonPropertyName("domain")]
        public Domain Domain { get; set; }
    }

    public class Domain
    {
        [JsonPropertyName("guid")]
        public string Guid { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
}