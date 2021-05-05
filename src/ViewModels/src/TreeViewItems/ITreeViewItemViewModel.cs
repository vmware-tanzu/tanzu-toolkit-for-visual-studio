using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Tanzu.Toolkit.ViewModels
{
    interface ITreeViewItemViewModel : INotifyPropertyChanged
    {
        ObservableCollection<TreeViewItemViewModel> Children { get; }
        bool IsExpanded { get; set; }
        bool IsSelected { get; set; }
        TreeViewItemViewModel Parent { get; }
        PlaceholderViewModel LoadingPlaceholder { get; }
        PlaceholderViewModel EmptyPlaceholder { get; }
        Task RefreshChildren();
    }
}