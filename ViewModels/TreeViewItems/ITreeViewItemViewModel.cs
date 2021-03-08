using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Tanzu.Toolkit.VisualStudio.ViewModels
{
    interface ITreeViewItemViewModel : INotifyPropertyChanged
    {
        ObservableCollection<TreeViewItemViewModel> Children { get; }
        bool IsExpanded { get; set; }
        bool IsSelected { get; set; }
        TreeViewItemViewModel Parent { get; }

        Task RefreshChildren();
    }
}