using System;
using System.Threading.Tasks;

namespace Tanzu.Toolkit.Services
{
    public interface IDispatcherService
    {
        void StartUiBackgroundPoller(Func<object, Task> method, object methodParam, int intervalInSeconds);
        void StopUiBackgroundUiPoller();
    }
}