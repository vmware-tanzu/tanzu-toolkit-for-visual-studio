using System.Collections.Generic;

namespace Tanzu.Toolkit.Models
{
    public class AppManifest
    {
        public int Version { get; set; }
        public List<AppConfig> Applications { get; set; }
    }

    public class AppConfig
    {
        public List<string> Buildpacks { get; set; }
        public string Command { get; set; }
        public string DiskQuota { get; set; }
        public DockerConfig Docker { get; set; }
        public Dictionary<string, string> Env { get; set; }
        public string HealthCheckHttpEndpoint { get; set; }
        public string HealthCheckType { get; set; }
        public int Instances { get; set; }
        public string Memory { get; set; }
        public MetadataConfig Metadata { get; set; }
        public string Name { get; set; }
        public bool NoRoute { get; set; }
        public string Path { get; set; }
        public List<ProcessConfig> Processes { get; set; }
        public bool RandomRoute { get; set; }
        public bool DefaultRoute { get; set; }
        public List<RouteConfig> Routes { get; set; }
        public List<string> Services { get; set; }
        public List<SidecarConfig> Sidecars { get; set; }
        public string Stack { get; set; }
    }

    public class DockerConfig
    {
        public string Image { get; set; }
        public string Username { get; set; }
    }

    public class MetadataConfig
    {
        public Dictionary<string, string> Annotations { get; set; }
        public Dictionary<string, string> Labels { get; set; }
    }

    public class ProcessConfig
    {
        public string Type { get; set; }
        public string Command { get; set; }
        public string DiskQuota { get; set; }
        public string HealthCheckHttpEndpoint { get; set; }
        public int HealthCheckInvocationTimeout { get; set; }
        public string HealthCheckType { get; set; }
        public int Instances { get; set; }
        public string Memory { get; set; }
        public int Timeout { get; set; }
    }

    public class RouteConfig
    {
        public string Route { get; set; }
        public string Protocol { get; set; }
    }

    public class ServiceConfig
    {
        public string Name { get; set; }
        public string BindingName { get; set; }
        public Dictionary<object, object> Parameters { get; set; }
    }

    public class SidecarConfig
    {
        public string Name { get; set; }
        public string Command { get; set; }
        public List<string> ProcessTypes { get; set; }
        public string Memory { get; set; }
    }
}
