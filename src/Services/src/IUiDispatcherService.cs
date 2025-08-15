using System;
using System.Threading.Tasks;

namespace Tanzu.Toolkit.Services
{
    public interface IUIDispatcherService
    {
        Task RunOnUIThreadAsync(Action method);
    }
}