using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Tanzu.Toolkit.ViewModels
{
    public interface ITreeViewItemViewModel : INotifyPropertyChanged
    {
        ObservableCollection<TreeViewItemViewModel> Children { get; }
        bool IsExpanded { get; set; }
        bool IsSelected { get; set; }
        TreeViewItemViewModel Parent { get; }
        PlaceholderViewModel LoadingPlaceholder { get; }
        PlaceholderViewModel EmptyPlaceholder { get; }
        ITasExplorerViewModel ParentTasExplorer { get; set; }
    }
}