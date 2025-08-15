using Tanzu.Toolkit.Models;

namespace Tanzu.Toolkit.Services.Serialization
{
    public interface ISerializationService
    {
        AppManifest ParseCfAppManifest(string manifestContents);

        string SerializeCfAppManifest(AppManifest manifest);
    }
}