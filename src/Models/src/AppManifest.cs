﻿using System;
using System.Collections.Generic;

namespace Tanzu.Toolkit.Models
{
    public class AppManifest
    {
        public int Version { get; set; }
        public List<AppConfig> Applications { get; set; }

        public AppManifest DeepClone()
        {
            var appsList = new List<AppConfig>();

            foreach (AppConfig app in Applications)
            {
                appsList.Add((AppConfig)app.Clone());
            }

            return new AppManifest
            {
                Version = Version,
                Applications = appsList,
            };
        }
    }

    public class AppConfig : ICloneable
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

        public object Clone()
        {
            // shallow copy first to replicate all value types
            AppConfig newApp = (AppConfig)MemberwiseClone();

            // complete deep copy by replicating all reference types

            if (Buildpacks != null)
            {
                var clonedBps = new List<string>();

                foreach (string bpName in Buildpacks)
                {
                    clonedBps.Add(bpName);
                }

                newApp.Buildpacks = clonedBps;
            }

            if (Docker != null)
            {
                var clonedDockerConfig = new DockerConfig
                {
                    Image = Docker.Image,
                    Username = Docker.Username,
                };

                newApp.Docker = clonedDockerConfig;
            }

            if (Env != null)
            {
                var clonedEnv = new Dictionary<string, string>();

                foreach (string key in Env.Keys)
                {
                    clonedEnv.Add(key, Env[key]);
                }

                newApp.Env = clonedEnv;
            }


            if (Metadata != null)
            {
                if (Metadata.Annotations != null)
                {
                    var clonedAnnotations = new Dictionary<string, string>();

                    foreach (string key in Metadata.Annotations.Keys)
                    {
                        clonedAnnotations.Add(key, Metadata.Annotations[key]);
                    }

                    newApp.Metadata.Annotations = clonedAnnotations;
                }

                if (Metadata.Labels != null)
                {
                    var clonedAnnotations = new Dictionary<string, string>();

                    foreach (string key in Metadata.Labels.Keys)
                    {
                        clonedAnnotations.Add(key, Metadata.Labels[key]);
                    }

                    newApp.Metadata.Labels = clonedAnnotations;
                }
            }

            if (Processes != null)
            {
                var newProcessList = new List<ProcessConfig>();

                foreach (ProcessConfig process in Processes)
                {
                    newProcessList.Add(new ProcessConfig
                    {
                        Command = process.Command,
                        DiskQuota = process.DiskQuota,
                        HealthCheckHttpEndpoint = process.HealthCheckHttpEndpoint,
                        HealthCheckInvocationTimeout = process.HealthCheckInvocationTimeout,
                        HealthCheckType = process.HealthCheckType,
                        Instances = process.Instances,
                        Memory = process.Memory,
                        Timeout = process.Timeout,
                        Type = process.Type,
                    });
                }

                newApp.Processes = newProcessList;
            }

            if (Routes != null)
            {
                var newRoutesList = new List<RouteConfig>();

                foreach (RouteConfig route in Routes)
                {
                    newRoutesList.Add(new RouteConfig
                    {
                        Protocol = route.Protocol,
                        Route = route.Route,
                    });
                }

                newApp.Routes = newRoutesList;
            }

            if (Sidecars != null)
            {
                var newSidecarList = new List<SidecarConfig>();

                foreach (SidecarConfig sidecar in Sidecars)
                {
                    var newSidecar = new SidecarConfig
                    {
                        Name = sidecar.Name,
                        Command = sidecar.Command,
                        Memory = sidecar.Memory,
                    };

                    if (sidecar.ProcessTypes != null)
                    {
                        var processTypeList = new List<string>();

                        foreach (string procType in sidecar.ProcessTypes)
                        {
                            processTypeList.Add(procType);
                        }

                        newSidecar.ProcessTypes = processTypeList;
                    }

                    newSidecarList.Add(newSidecar);
                }

                newApp.Sidecars = newSidecarList;
            }

            return newApp;
        }
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

    public class SidecarConfig
    {
        public string Name { get; set; }
        public string Command { get; set; }
        public List<string> ProcessTypes { get; set; }
        public string Memory { get; set; }
    }
}
