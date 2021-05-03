using System;

namespace Tanzu.Toolkit.VisualStudio.WpfViews.Services
{
    public interface IVisualStudioService
    {
        void DisplayToolWindowForView(Type viewType);
    }
}