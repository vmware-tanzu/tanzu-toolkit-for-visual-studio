using System;
using System.Threading.Tasks;

namespace Tanzu.Toolkit.Services.Threading
{
    public interface IThreadingService
    {
        void StartTask(Func<Task> method);
    }
}