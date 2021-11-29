using System;

namespace Tanzu.Toolkit.VisualStudio.Services
{
    public interface IViewService
    {
        void DisplayViewByType(Type viewType);
    }
}