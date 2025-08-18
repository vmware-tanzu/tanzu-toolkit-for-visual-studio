using System.Diagnostics;
using System.Threading.Tasks;
using Tanzu.Toolkit.Models;

namespace Tanzu.Toolkit.ViewModels
{
    public interface IOutputViewModel
    {
        Process ActiveProcess { get; set; }

        bool OutputPaused { get; set; }

        bool OutputIsAppLogs { get; set; }

        void AppendLine(string newContent);

        Task BeginStreamingAppLogsForAppAsync(CloudFoundryApp cfApp, IView outputView);

        void CancelActiveProcess(object arg = null);

        void ClearContent(object arg = null);

        void PauseOutput(object arg = null);

        Task ResumeOutputAsync(object arg = null);
    }
}