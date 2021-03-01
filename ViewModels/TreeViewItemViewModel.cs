using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Tanzu.Toolkit.VisualStudio.ViewModels
{
    public class TreeViewItemViewModel : AbstractViewModel, ITreeViewItemViewModel
    {
        private bool _isExpanded;
        private bool _isSelected;
        private string _text;
        private TreeViewItemViewModel _parent;
        private ObservableCollection<TreeViewItemViewModel> _children;
        private bool _loadChildrenOnExpansion;
        private TreeViewItemViewModel placeholderChild;

        protected TreeViewItemViewModel(TreeViewItemViewModel parent, IServiceProvider services)
            : base(services)
        {
            _parent = parent;
            _isExpanded = false;
            _children = new ObservableCollection<TreeViewItemViewModel>
            {
                PlaceholderChild
            };
            _loadChildrenOnExpansion = true;
        }

        /// <summary>
        /// Gets/sets the text that is displayed 
        /// on the tree view for this item.
        /// </summary>
        public string DisplayText
        {
            get { return _text; }
            set
            {
                _text = value;
                RaisePropertyChangedEvent("DisplayText");
            }
        }

        public bool LoadChildrenOnExpansion
        {
            get => _loadChildrenOnExpansion;
            set => _loadChildrenOnExpansion = value;
        }

        /// <summary>
        /// Gets/sets whether the TreeViewItem 
        /// associated with this object is expanded.
        /// </summary>
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (value != _isExpanded)
                {
                    _isExpanded = value;
                    if (value == true && LoadChildrenOnExpansion)
                    {
                        if (HasPlaceholderChild) Children.Remove(PlaceholderChild);

                        // Lazily load the child items
                        LoadChildren(); 
                    }
                    RaisePropertyChangedEvent("IsExpanded");
                }
            }
        }

        // placeholder to allow this tree view item to be expandable before its children 
        // have loaded or if it has no children (the presence of children causes the 
        // expansion button to appear)
        public TreeViewItemViewModel PlaceholderChild
        {
            get { return placeholderChild; }
            set { placeholderChild = value; }
        }

        /// <summary>
        /// Returns true if this object's Children have not yet been populated.
        /// </summary>
        public bool HasPlaceholderChild
        {
            get { return Children.Count == 1 && Children[0] == PlaceholderChild; }
        }

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (value != _isSelected)
                {
                    _isSelected = value;
                    RaisePropertyChangedEvent("IsSelected");
                }
            }
        }

        public TreeViewItemViewModel Parent
        {
            get { return _parent; }
            set
            {
                _parent = value;
                RaisePropertyChangedEvent("Parent");
            }
        }

        public ObservableCollection<TreeViewItemViewModel> Children
        {
            get => _children;
            set
            {
                _children = value;
                RaisePropertyChangedEvent("Children");
            }
        }

        /// <summary>
        /// Invoked when the child items need to be loaded on demand.
        /// Subclasses can override this to populate the Children collection.
        /// </summary>
        protected virtual async Task LoadChildren()
        {
        }

        public async Task RefreshChildren()
        {
            await LoadChildren();
        }

    }
}
