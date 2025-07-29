using System.Collections.Generic;
using Tanzu.Toolkit.Models;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.ObjectGraphVisitors;

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
                .WithEmissionPhaseObjectGraphVisitor(args => new EmptyCollectionSkipper(args.InnerVisitor))
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
            if (manifestApp.Buildpacks is { Count: 1 }
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

    public sealed class EmptyCollectionSkipper : ChainedObjectGraphVisitor
    {
        public EmptyCollectionSkipper(IObjectGraphVisitor<IEmitter> nextVisitor)
            : base(nextVisitor)
        {
        }

        public override bool EnterMapping(IPropertyDescriptor key, IObjectDescriptor value, IEmitter context, ObjectSerializer serializer)
        {
            var retVal = false;

            if (value.Value != null)
                return false;

            if (typeof(System.Collections.IEnumerable).IsAssignableFrom(value.Value.GetType()))
            {   // We have a collection
                var enumerableObject = (System.Collections.IEnumerable)value.Value;
                if (enumerableObject.GetEnumerator().MoveNext()) // Returns true if the collection is not empty.
                {   // Don't skip this item - serialize it as normal.
                    retVal = base.EnterMapping(key, value, context, serializer);
                }
                // Else we have an empty collection and the initialized return value of false is correct.
            }
            else
            {   // Not a collection, normal serialization.
                retVal = base.EnterMapping(key, value, context, serializer);
            }

            return retVal;
        }
    }
}
