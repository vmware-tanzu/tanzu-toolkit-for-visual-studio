using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace Tanzu.Toolkit.Services.Threading
{
    public class ThreadingService : IThreadingService
    {
        private bool _isPolling = false;
        private readonly IUiDispatcherService _dispatcherService;

        public ThreadingService(IUiDispatcherService dispatcherService)
        {
            _dispatcherService = dispatcherService;
        }

        public bool IsPolling
        {
            get => _isPolling;

            set => _isPolling = value;
        }

        public Task StartBackgroundTask(Func<Task> method)
        {
            return method.Invoke();
        }

        public Task StartBackgroundTask(Func<Task> method, CancellationToken cancellationToken)
        {
            return Task.Run(method, cancellationToken);
        }

        public void StartRecurrentUiTaskInBackground(Action<object> action, object param, int intervalInSeconds)
        {
            if (!IsPolling)
            {
                IsPolling = true;
                var pollingTask = new Task(async () =>
                {
                    while (IsPolling)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(intervalInSeconds));
                        ExecuteInUIThread(action, param);
                    }
                });
                pollingTask.Start();
            }
        }

        public void ExecuteInUIThread(Action method)
        {
            _dispatcherService.RunOnUiThreadAsync(method);
        }

        public void ExecuteInUIThread(Action<object> method, object methodParam)
        {
            _dispatcherService.RunOnUiThreadAsync(() => method.Invoke(methodParam));
        }

        public async Task ExecuteInUIThreadAsync(Action method)
        {
            await _dispatcherService.RunOnUiThreadAsync(method);
        }

        public async Task AddItemToCollectionOnUiThreadAsync<T>(ObservableCollection<T> list, T item)
        {
            await _dispatcherService.RunOnUiThreadAsync(() => list.Add(item));
        }

        public async Task RemoveItemFromCollectionOnUiThreadAsync<T>(ObservableCollection<T> list, T item)
        {
            await _dispatcherService.RunOnUiThreadAsync(() => list.Remove(item));
        }
    }
}
