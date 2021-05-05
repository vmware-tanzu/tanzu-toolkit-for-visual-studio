using System;

namespace Tanzu.Toolkit.ViewModels
{
    public class PlaceholderViewModel : TreeViewItemViewModel
    {
        public PlaceholderViewModel(TreeViewItemViewModel parent, IServiceProvider services) : base (parent, services, childless: true)
        {
        }
    }
}
