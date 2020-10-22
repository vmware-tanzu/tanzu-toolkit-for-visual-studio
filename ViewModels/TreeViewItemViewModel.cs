using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace TanzuForVS.ViewModels
{
    public class TreeViewItemViewModel : AbstractViewModel, ITreeViewItemViewModel
    {
        private bool _isExpanded;
        private bool _isSelected;
        private string _text;
        private TreeViewItemViewModel _parent;
        private ObservableCollection<TreeViewItemViewModel> _children;

        // placeholder to allow this tree view item to be expandable before its children 
        // have loaded (the presence of children causes the expansion button to appear)
        private readonly TreeViewItemViewModel DummyChild;

        protected TreeViewItemViewModel(TreeViewItemViewModel parent, IServiceProvider services)
            : base(services)
        {
            _parent = parent;
            _children = new ObservableCollection<TreeViewItemViewModel>
            {
                DummyChild
            };
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
                this.RaisePropertyChangedEvent("DisplayText");
            }
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
                    this.RaisePropertyChangedEvent("IsExpanded");
                }

                // Expand all the way up to the root.
                if (_isExpanded && _parent != null)
                    _parent.IsExpanded = true;

                // Lazy load the child items, if necessary.
                if (this.HasDummyChild)
                {
                    this.Children.Remove(DummyChild);
                }

                if (value == true) this.LoadChildren();
            }
        }

        /// <summary>
        /// Returns true if this object's Children have not yet been populated.
        /// </summary>
        public bool HasDummyChild
        {
            get { return this.Children.Count == 1 && this.Children[0] == DummyChild; }
        }

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (value != _isSelected)
                {
                    _isSelected = value;
                    this.RaisePropertyChangedEvent("IsSelected");
                }
            }
        }

        public TreeViewItemViewModel Parent
        {
            get { return this._parent; }
            set
            {
                this._parent = value;
                this.RaisePropertyChangedEvent("Parent");
            }
        }

        public ObservableCollection<TreeViewItemViewModel> Children
        {
            get => _children;
            set
            {
                _children = value;
                this.RaisePropertyChangedEvent("Children");
            }
        }

        /// <summary>
        /// Invoked when the child items need to be loaded on demand.
        /// Subclasses can override this to populate the Children collection.
        /// </summary>
        protected virtual async Task LoadChildren()
        {
        }

    }
}
