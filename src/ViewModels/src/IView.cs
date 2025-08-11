using System;

namespace Tanzu.Toolkit.ViewModels
{
    public interface IView
    {
        IViewModel ViewModel { get; }
        Action DisplayView { get; set; }
    }
}