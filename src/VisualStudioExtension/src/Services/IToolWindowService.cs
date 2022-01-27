using System;

namespace Tanzu.Toolkit.VisualStudio.Services
{
    public interface IToolWindowService
    {
        object CreateToolWindowForView(Type viewType, string caption);
    }
}