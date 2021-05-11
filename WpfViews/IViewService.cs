using System;

namespace Tanzu.Toolkit.VisualStudio.WpfViews.Services
{
    public interface IViewService
    {
        void DisplayViewByType(Type viewType);
    }
}