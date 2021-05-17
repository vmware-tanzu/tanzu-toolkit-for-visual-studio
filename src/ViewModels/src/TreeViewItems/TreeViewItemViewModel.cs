using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Tanzu.Toolkit.Services.Threading;

namespace Tanzu.Toolkit.ViewModels
{
    public class TreeViewItemViewModel : AbstractViewModel, ITreeViewItemViewModel
    {
        internal const string _defaultLoadingMsg = "Loading ...";
        private bool _isExpanded;
        private bool _isSelected;
        private string _text;
        private TreeViewItemViewModel _parent;
        private ObservableCollection<TreeViewItemViewModel> _children;
        private bool _isLoading;
        private IThreadingService _threadingService; 

        protected TreeViewItemViewModel(TreeViewItemViewModel parent, IServiceProvider services, bool childless = false)
            : base(services)
        {
            _parent = parent;
            _isExpanded = false;
            _isLoading = false;

            _threadingService = services.GetRequiredService<IThreadingService>();

            if (!childless) // only create placeholder & assign children if this vm isn't a placeholder itself
            {
                LoadingPlaceholder = new PlaceholderViewModel(parent: this, services)
                {
                    DisplayText = _defaultLoadingMsg,
                };

                _children = new ObservableCollection<TreeViewItemViewModel>
                {
                    LoadingPlaceholder,
                };
            }
        }

        /// <summary>
        /// Gets or sets the text that is displayed
        /// on the tree view for this item.
        /// </summary>
        public string DisplayText
        {
            get
            {
                return _text;
            }

            set
            {
                _text = value;
                RaisePropertyChangedEvent("DisplayText");
            }
        }

        public bool IsLoading { get => _isLoading; set => _isLoading = value; }

        public bool HasEmptyPlaceholder { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the TreeViewItem
        /// associated with this object is expanded.
        /// </summary>
        public bool IsExpanded
        {
            get
            {
                return _isExpanded;
            }

            set
            {
                if (value != _isExpanded)
                {
                    _isExpanded = value;

                    if (value == true && !IsLoading)
                    {
                        IsLoading = true;

                        Children = new ObservableCollection<TreeViewItemViewModel>
                        {
                            LoadingPlaceholder,
                        };

                        // Lazily load the child items @ expansion time in a separate thread
                        _threadingService.StartTask(LoadChildren);
                    }

                    RaisePropertyChangedEvent("IsExpanded");
                }
            }
        }

        public bool IsSelected
        {
            get
            {
                return _isSelected;
            }

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
            get
            {
                return _parent;
            }

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

        public virtual PlaceholderViewModel LoadingPlaceholder { get; protected set; }

        public virtual PlaceholderViewModel EmptyPlaceholder { get; protected set; }

        public async Task RefreshChildren()
        {
            await LoadChildren();
        }

        /// <summary>
        /// Invoked when the child items need to be loaded on demand.
        /// Subclasses can override this to populate the Children collection.
        /// </summary>
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        protected internal virtual async Task LoadChildren()
        {
        }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }
}
