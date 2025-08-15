using System;
using System.Threading.Tasks;
using Tanzu.Toolkit.Services;

namespace Tanzu.Toolkit.VisualStudio.Services
{
    public class VisualStudioUIDispatcherService : IUIDispatcherService
    {
        public async Task RunOnUIThreadAsync(Action method)
        {
            await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            method.Invoke();
        }
    }
}