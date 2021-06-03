using System;
using System.Threading.Tasks;
using System.Windows;
using Tanzu.Toolkit.Services;

namespace Tanzu.Toolkit.VisualStudio.WpfViews.Services
{
    public class WpfDispatcherService : IDispatcherService
    {
        private bool _isPolling = false;

        public bool IsPolling
        {
            get => _isPolling; 

            set { _isPolling = value; }
        }

        public void StartUiBackgroundPoller(Action<object> pollingMethod, object methodParam, int intervalInSeconds)
        {
            var uiDispatcher = Application.Current.Dispatcher;

            if (!IsPolling)
            {
                IsPolling = true;

                var pollingTask = new Task(async () =>
                {
                    while (IsPolling)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(intervalInSeconds));

                        uiDispatcher.Invoke(() =>
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
