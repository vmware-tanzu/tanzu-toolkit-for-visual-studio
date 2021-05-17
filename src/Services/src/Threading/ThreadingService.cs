using System;
using System.Threading.Tasks;

namespace Tanzu.Toolkit.Services.Threading
{
    public class ThreadingService : IThreadingService
    {
        public ThreadingService()
        {
        }

        public void StartTask(Func<Task> method)
        {
            Task.Run(method);
        }
    }
}
