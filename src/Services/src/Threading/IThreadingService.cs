using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Tanzu.Toolkit.Services.Threading
{
    public interface IThreadingService
    {
        bool IsPolling { get; set; }

        Task AddItemToCollectionOnUiThreadAsync<T>(ObservableCollection<T> list, T item);
        void ExecuteInUIThread(Action method);
        Task ExecuteInUIThreadAsync(Action method);
        Task RemoveItemFromCollectionOnUiThreadAsync<T>(ObservableCollection<T> list, T item);
        Task ReplaceCollectionOnUiThreadAsync<T>(ObservableCollection<T> collectionToReplace, ObservableCollection<T> newCollection);
        Task StartBackgroundTask(Func<Task> method);
        void StartRecurrentUiTaskInBackground(Action<object> pollingMethod, object methodParam, int intervalInSeconds);
    }
}