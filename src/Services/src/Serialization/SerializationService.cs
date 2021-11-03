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
                .WithNamingConvention(CfAppManifestNamingConvention.Instance)
                .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults)
                .Build();

            _cfAppManifestParser = new DeserializerBuilder()
                .WithNamingConvention(CfAppManifestNamingConvention.Instance)
                .Build();
        }

        public AppManifest ParseCfAppManifest(string manifestContents)
        {
            return _cfAppManifestParser.Deserialize<AppManifest>(manifestContents);
        }

        public string SerializeCfAppManifest(AppManifest manifest)
        {
            return _cfAppManifestSerializer.Serialize(manifest);
        }
    }
}
