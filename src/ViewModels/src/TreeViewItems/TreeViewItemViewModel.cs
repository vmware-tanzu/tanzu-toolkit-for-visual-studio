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
        private readonly IThreadingService _threadingService;

        protected TreeViewItemViewModel(TreeViewItemViewModel parent, ITanzuExplorerViewModel parentTanzuExplorer, IServiceProvider services, bool childless = false, bool expanded = false)
            : base(services)
        {
            _parent = parent;
            _isExpanded = expanded;
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

            ParentTanzuExplorer = parentTanzuExplorer;
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

        public bool IsLoading
        {
            get => _isLoading;

            set
            {
                _isLoading = value;
                if (_isLoading)
                {
                    if (Children.Count == 0)
                    {
                        Children = new ObservableCollection<TreeViewItemViewModel>
                        {
                            LoadingPlaceholder,
                        };
                    }
                }
                else
                {
                    ThreadingService.ExecuteInUIThread(() => Children.Remove(LoadingPlaceholder));
                }
                RaisePropertyChangedEvent("IsLoading");
            }
        }

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
                    if (_isExpanded & !IsLoading)
                    {
                        // Lazily load child items in a separate thread @ expansion time
                        _threadingService.StartBackgroundTask(UpdateAllChildren);
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

        public ITanzuExplorerViewModel ParentTanzuExplorer { get; set; }

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

        protected internal virtual async Task UpdateAllChildren()
        {
            // await to suppress aync warning
            await Task.Run(() => Logger.Error("TreeViewItemViewModel.UpdateAllChildren was called; this method should only ever be run by classes that inherit from TreeViewItemViewModel."));
        }
    }
}
