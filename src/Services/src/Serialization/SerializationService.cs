using System.Collections.Generic;
using Tanzu.Toolkit.Models;
using YamlDotNet.Serialization;

namespace Tanzu.Toolkit.Services
{
    public class SerializationService : ISerializationService
    {
        private readonly ISerializer _cfAppManifestSerializer;
        private readonly IDeserializer _cfAppManifestParser;

        public SerializationService()
        {
            _cfAppManifestSerializer = new SerializerBuilder()
                .WithNamingConvention(CfAppManifestNamingConvention._instance)
                .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults)
                .Build();

            _cfAppManifestParser = new DeserializerBuilder()
                .WithNamingConvention(CfAppManifestNamingConvention._instance)
                .Build();
        }

        public AppManifest ParseCfAppManifest(string manifestContents)
        {
            var manifest = _cfAppManifestParser.Deserialize<AppManifest>(manifestContents);
            var manifestApp = manifest.App;

            if (manifestApp.Buildpacks == null && manifestApp.Buildpack != null)
            {
                manifest.OriginalBuildpackScheme = AppManifest.BuildpackScheme.Singular;
                ReformatSingularBuildpackAsList(manifest, manifestApp.Buildpack);
            }

            return manifest;
        }

        public string SerializeCfAppManifest(AppManifest manifest)
        {
            var manifestApp = manifest.App;
            if (manifestApp.Buildpacks != null
                && manifestApp.Buildpacks.Count == 1
                && manifest.OriginalBuildpackScheme == AppManifest.BuildpackScheme.Singular)
            {
                ReformatBuildpacksListAsSingularBuildpack(manifest, manifestApp.Buildpacks[0]);
            }

            return _cfAppManifestSerializer.Serialize(manifest);
        }

        private static void ReformatSingularBuildpackAsList(AppManifest manifest, string singleBuildpackName)
        {
            manifest.Applications[0].Buildpacks ??= new List<string>();
            manifest.Applications[0].Buildpacks.Add(singleBuildpackName);
            manifest.Applications[0].Buildpack = null;
        }

        private static void ReformatBuildpacksListAsSingularBuildpack(AppManifest manifest, string singleBuildpackName)
        {
            manifest.Applications[0].Buildpacks = null;
            manifest.Applications[0].Buildpack = singleBuildpackName;
        }
    }
}
