using System;
using System.Threading.Tasks;
using Tanzu.Toolkit.ViewModels;

namespace Tanzu.Toolkit.VisualStudio.Services
{
    public interface IToolWindowService
    {
        Task<IView> CreateToolWindowForViewAsync(Type viewType, string caption);
    }
}