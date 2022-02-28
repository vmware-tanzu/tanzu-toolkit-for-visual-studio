using System;
using System.Threading.Tasks;

namespace Tanzu.Toolkit.Services
{
    public interface IUiDispatcherService
    {
        Task RunOnUiThreadAsync(Action method);
    }
}
