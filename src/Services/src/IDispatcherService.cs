using System;
using System.Threading.Tasks;

namespace Tanzu.Toolkit.Services
{
    public interface IDispatcherService
    {
        bool IsPolling { get; set; }

        void StartUiBackgroundPoller(Action<object> pollingMethod, object methodParam, int intervalInSeconds);
    }
}