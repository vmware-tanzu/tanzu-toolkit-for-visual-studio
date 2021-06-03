using System;
using System.Threading.Tasks;

namespace Tanzu.Toolkit.Services.Threading
{
    public class ThreadingService : IThreadingService
    {
        private bool _isPolling = false;
        private IUiDispatcherService _dispatcherService;

        public bool IsPolling
        {
            get => _isPolling;

            set { _isPolling = value; }
        }

        public ThreadingService(IUiDispatcherService dispatcherService)
        {
            _dispatcherService = dispatcherService;
        }

        public void StartTask(Func<Task> method)
        {
            Task.Run(method);
        }

        public void StartUiBackgroundPoller(Action<object> pollingMethod, object methodParam, int intervalInSeconds)
        {
            if (!IsPolling)
            {
                IsPolling = true;

                var pollingTask = new Task(async () =>
                {
                    while (IsPolling)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(intervalInSeconds));

                        _dispatcherService.RunOnUiThread(() =>
                        {
                            pollingMethod(methodParam);
                        });
                    }
                });

                pollingTask.Start();
            }
        }
    }
}
