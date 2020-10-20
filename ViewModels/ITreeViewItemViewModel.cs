using System.Collections.ObjectModel;
using System.ComponentModel;
using TanzuForVS.ViewModels;

interface ITreeViewItemViewModel : INotifyPropertyChanged
{
    ObservableCollection<TreeViewItemViewModel> Children { get; }
    bool HasDummyChild { get; }
    bool IsExpanded { get; set; }
    bool IsSelected { get; set; }
    TreeViewItemViewModel Parent { get; }
}