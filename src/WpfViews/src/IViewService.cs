using System;

namespace Tanzu.Toolkit.WpfViews.Services
{
    public interface IViewService
    {
        void DisplayViewByType(Type viewType);
    }
}