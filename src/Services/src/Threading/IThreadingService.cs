using System;
using System.Threading.Tasks;

namespace Tanzu.Toolkit.Services.Threading
{
    public interface IThreadingService
    {
        bool IsPolling { get; set; }

        void StartTask(Func<Task> method);
        void StartUiBackgroundPoller(Action<object> pollingMethod, object methodParam, int intervalInSeconds);
    }
}