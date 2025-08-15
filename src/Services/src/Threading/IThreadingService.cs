using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace Tanzu.Toolkit.Services.Threading
{
    public interface IThreadingService
    {
        bool IsPolling { get; set; }

        Task AddItemToCollectionOnUIThreadAsync<T>(ObservableCollection<T> list, T item);

        void ExecuteInUIThread(Action method);

        Task ExecuteInUIThreadAsync(Action method);

        Task RemoveItemFromCollectionOnUIThreadAsync<T>(ObservableCollection<T> list, T item);

        Task StartBackgroundTaskAsync(Func<Task> method);

        Task StartBackgroundTaskAsync(Func<Task> method, CancellationToken cancellationToken);

        void StartRecurrentUITaskInBackground(Action<object> pollingMethod, object methodParam, int intervalInSeconds);
    }
}