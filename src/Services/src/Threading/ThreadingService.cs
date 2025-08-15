using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace Tanzu.Toolkit.Services.Threading
{
    public class ThreadingService : IThreadingService
    {
        private readonly IUIDispatcherService _dispatcherService;

        public ThreadingService(IUIDispatcherService dispatcherService)
        {
            _dispatcherService = dispatcherService;
        }

        public bool IsPolling { get; set; }

        public Task StartBackgroundTaskAsync(Func<Task> method)
        {
            return method.Invoke();
        }

        public Task StartBackgroundTaskAsync(Func<Task> method, CancellationToken cancellationToken)
        {
            return Task.Run(method, cancellationToken);
        }

        public void StartRecurrentUITaskInBackground(Action<object> action, object param, int intervalInSeconds)
        {
            if (IsPolling)
            {
                return;
            }

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

        public void ExecuteInUIThread(Action method)
        {
            _dispatcherService.RunOnUIThreadAsync(method);
        }

        public void ExecuteInUIThread(Action<object> method, object methodParam)
        {
            _dispatcherService.RunOnUIThreadAsync(() => method.Invoke(methodParam));
        }

        public async Task ExecuteInUIThreadAsync(Action method)
        {
            await _dispatcherService.RunOnUIThreadAsync(method);
        }

        public async Task AddItemToCollectionOnUIThreadAsync<T>(ObservableCollection<T> list, T item)
        {
            await _dispatcherService.RunOnUIThreadAsync(() => list.Add(item));
        }

        public async Task RemoveItemFromCollectionOnUIThreadAsync<T>(ObservableCollection<T> list, T item)
        {
            await _dispatcherService.RunOnUIThreadAsync(() => list.Remove(item));
        }
    }
}