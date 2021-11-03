using Tanzu.Toolkit.Models;

namespace Tanzu.Toolkit.Services
{
    public interface ISerializationService
    {
        AppManifest ParseCfAppManifest(string manifestContents);
        string SerializeCfAppManifest(AppManifest manifest);
    }
}