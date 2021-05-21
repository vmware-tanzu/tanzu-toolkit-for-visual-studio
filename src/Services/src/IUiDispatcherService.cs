using System;

namespace Tanzu.Toolkit.Services
{
    public interface IUiDispatcherService
    {
        void RunOnUiThread(Action method);
    }
}
