using System.Threading.Tasks;

namespace Tanzu.Toolkit.Services.DotnetCli
{
    public interface IDotnetCliService
    {
        Task<bool> PublishProjectForRemoteDebuggingAsync(string projectDir, string targetFrameworkMoniker, string runtimeIdentifier, string configuration, string outputDirName);
    }
}