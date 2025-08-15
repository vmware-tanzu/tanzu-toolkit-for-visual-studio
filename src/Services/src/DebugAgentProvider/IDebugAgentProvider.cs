using System.Threading.Tasks;
using Tanzu.Toolkit.Models;

namespace Tanzu.Toolkit.Services.DebugAgentProvider
{
    public interface IDebugAgentProvider
    {
        Task<DetailedResult> InstallVsdbgForCFAppAsync(CloudFoundryApp app);
    }
}