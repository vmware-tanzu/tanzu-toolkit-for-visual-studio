using System;
using System.Threading.Tasks;
using System.Windows;
using Tanzu.Toolkit.Services;

namespace Tanzu.Toolkit.VisualStudio.WpfViews.Services
{
    public class WpfDispatcherService : IDispatcherService
    {
        private bool _isPolling = false;

        public void StartUiBackgroundPoller(Func<object, Task> method, object methodParam, int intervalInSeconds)
        {
            var uiDispatcher = Application.Current.Dispatcher;

            if (!_isPolling)
            {
                _isPolling = true;

                var pollingTask = new Task(async () =>
                {
                    while (_isPolling)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(intervalInSeconds));

                        await uiDispatcher.InvokeAsync(async () =>
                        {
                            await method(methodParam);
                        });
                    }
                });

                pollingTask.Start();
            }
        }

        public void StopUiBackgroundUiPoller()
        {
            if (_isPolling)
            {
                _isPolling = false;
            }
        }
    }
}
