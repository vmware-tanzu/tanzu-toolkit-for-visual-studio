using System;
using Tanzu.Toolkit.ViewModels;

namespace Tanzu.Toolkit.VisualStudio.Services
{
    public interface IToolWindowService
    {
        IView CreateToolWindowForView(Type viewType, string caption);
    }
}