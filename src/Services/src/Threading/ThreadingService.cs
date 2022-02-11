using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Tanzu.Toolkit.Services.Threading
{
    public class ThreadingService : IThreadingService
    {
        private bool _isPolling = false;
        private IUiDispatcherService _dispatcherService;

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

        public async Task ReplaceCollectionOnUiThreadAsync<T>(ObservableCollection<T> collectionToReplace, ObservableCollection<T> newCollection)
        {
            var removalTasks = new List<Task>();
            foreach (var item in collectionToReplace)
            {
                removalTasks.Add(RemoveItemFromCollectionOnUiThreadAsync(collectionToReplace, item));
            }
            await Task.WhenAll(removalTasks);

            var additionTasks = new List<Task>();
            foreach (var item in newCollection)
            {
                additionTasks.Add(AddItemToCollectionOnUiThreadAsync(collectionToReplace, item));
            }
            await Task.WhenAll(additionTasks);
        }
    }
}
